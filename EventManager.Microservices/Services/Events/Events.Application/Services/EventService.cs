using Events.Application.DTOs;
using Events.Application.Interfaces;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Events.Application.Services;

/// <summary>
/// Реализация use case сервиса мероприятий.
/// Содержит бизнес-логику CRUD-операций над мероприятиями.
/// Зависит только от Domain (сущности, исключения) и интерфейсов портов (IEventRepository).
/// </summary>
public class EventService : IEventService
{
	private readonly IEventRepository _eventRepo;
	private readonly ICacheService _cache;
	private readonly CacheSettings _cacheSettings;
	private readonly ILogger<EventService> _logger;

	/// <summary>
	/// Ключи кеша храним в одном месте.
	/// </summary>
	private static class CacheKeys
	{
		public static string EventById(Guid id) => $"event:{id}";
		public const string TopEvents = "events:top10";
	}

	/// <summary>
	/// Конструктор с внедрением зависимостей.
	/// </summary>
	/// <param name="eventRepo">Порт репозитория мероприятий (реализация в Infrastructure).</param>
	/// <param name="logger">Логгер для записи отладочной информации.</param>
	public EventService(IEventRepository eventRepo,
									ILogger<EventService> logger,
									ICacheService cache,
									IOptions<CacheSettings> cacheSettings)
	{
		_eventRepo = eventRepo ?? throw new ArgumentNullException(nameof(eventRepo));
		 _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public async Task<PaginatedResultDto<EventResponseDto>> GetFiltered(GetEventsQueryParams query)
	{
		_logger.LogInformation("Запрос списка событий");

		// Делегируем фильтрацию репозиторию (Infrastructure)
		var (items, totalCount) = await _eventRepo.GetFilteredAsync(
			query.Title, query.From, query.To, query.Page, query.PageSize, CancellationToken.None);

		// Маппим доменные сущности в DTO
		return new PaginatedResultDto<EventResponseDto>
		{
			TotalCount = totalCount,
			Page = query.Page,
			PageSize = query.PageSize,
			Items = items.Select(MapToResponse).ToList()
		};
	}

	/// <inheritdoc/>
	public async Task<EventResponseDto> GetById(Guid id)
	{
		var cacheKey = CacheKeys.EventById(id);

		// 1. Пытаемся взять из кеша
		var cachedEvent = await _cache.GetAsync<EventResponseDto>(cacheKey, CancellationToken.None);
		if (cachedEvent != null)
		{
			_logger.LogInformation("Событие {EventId} найдено в кеше.", id);
			return cachedEvent;
		}

		// 2. Событие в кеше не найдено - идем в БД
		_logger.LogInformation("Промах кеша для события {EventId}. Запрос в БД.", id);
		var eventEntity = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		var responseDto = MapToResponse(eventEntity);

		// 3. Сохраняем в кеш
		await _cache.SetAsync(cacheKey, responseDto, _cacheSettings.EventByIdTtl, CancellationToken.None);

		return responseDto;
	}

	/// <inheritdoc/>
	public async Task<List<EventResponseDto>> GetTopEvents(int count)
	{
		var cacheKey = CacheKeys.TopEvents;

		// 1. Пытаемся взять из кеша
		var cachedTop = await _cache.GetAsync<List<EventResponseDto>>(cacheKey, CancellationToken.None);
		if (cachedTop != null)
		{
			_logger.LogInformation("Топ событий найден в кеше.");
			return cachedTop;
		}

		// 2. Промах кеша - идем в БД
		_logger.LogInformation("Промах кеша для топа событий. Запрос в БД.");
		var events = await _eventRepo.GetTopEventsAsync(count, CancellationToken.None);
		var responseDtos = events.Select(MapToResponse).ToList();

		// 3. Сохраняем в кеш
		await _cache.SetAsync(cacheKey, responseDtos, _cacheSettings.TopEventsTtl, CancellationToken.None);

		return responseDtos;
	}

	/// <inheritdoc/>
	public async Task<EventResponseDto> Create(CreateEventRequestDto dto)
	{
		// Используем фабричный метод доменной сущности (валидация внутри)
		var newEvent = Event.Create(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
		_eventRepo.Add(newEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);

		// Инвалидируем топ при создании нового события, так как оно может попасть в топ
		await _cache.RemoveAsync(CacheKeys.TopEvents, CancellationToken.None);

		return MapToResponse(newEvent);
	}

	/// <inheritdoc/>
	public async Task Update(Guid id, UpdateEventRequestDto dto)
	{
		var existingEvent = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		existingEvent.UpdateDetails(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);

		// 1. Сначала сохраняем в БД
		_eventRepo.Update(existingEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);

		// 2. Затем инвалидируем кеш (стратегия Cache Invalidation)
		await _cache.RemoveAsync(CacheKeys.EventById(id), CancellationToken.None);

		// Также инвалидируем топ, так как количество мест могло измениться, что влияет на рейтинг
		await _cache.RemoveAsync(CacheKeys.TopEvents, CancellationToken.None);
	}

	/// <inheritdoc/>
	public async Task Delete(Guid id)
	{
		var existingEvent = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		_eventRepo.Remove(existingEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);

		// Инвалидация кешей
		await _cache.RemoveAsync(CacheKeys.EventById(id), CancellationToken.None);
		await _cache.RemoveAsync(CacheKeys.TopEvents, CancellationToken.None);
	}

	/// <summary>
	/// Приватный метод маппинга доменной сущности Event в DTO ответа.
	/// </summary>
	private static EventResponseDto MapToResponse(Event e) => new()
	{
		Id = e.Id,
		Title = e.Title,
		Description = e.Description,
		StartAt = e.StartAt,
		EndAt = e.EndAt,
		TotalSeats = e.TotalSeats,
		AvailableSeats = e.AvailableSeats
	};
}