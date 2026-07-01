using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventManager.Application.Services;

/// <summary>
/// Реализация use case сервиса мероприятий.
/// Содержит бизнес-логику CRUD-операций над мероприятиями.
/// Зависит только от Domain (сущности, исключения) и интерфейсов портов (IEventRepository).
/// </summary>
public class EventService : IEventService
{
	private readonly IEventRepository _eventRepo;
	private readonly ILogger<EventService> _logger;

	/// <summary>
	/// Конструктор с внедрением зависимостей.
	/// </summary>
	/// <param name="eventRepo">Порт репозитория мероприятий (реализация в Infrastructure).</param>
	/// <param name="logger">Логгер для записи отладочной информации.</param>
	public EventService(IEventRepository eventRepo, ILogger<EventService> logger)
	{
		_eventRepo = eventRepo ?? throw new ArgumentNullException(nameof(eventRepo));
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
		var eventEntity = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");
		return MapToResponse(eventEntity);
	}

	/// <inheritdoc/>
	public async Task<EventResponseDto> Create(CreateEventRequestDto dto)
	{
		// Используем фабричный метод доменной сущности (валидация внутри)
		var newEvent = Event.Create(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
		_eventRepo.Add(newEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);
		return MapToResponse(newEvent);
	}

	/// <inheritdoc/>
	public async Task Update(Guid id, UpdateEventRequestDto dto)
	{
		var existingEvent = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		// Бизнес-правило инкапсулировано в доменной сущности
		existingEvent.UpdateDetails(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
		_eventRepo.Update(existingEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);
	}

	/// <inheritdoc/>
	public async Task Delete(Guid id)
	{
		var existingEvent = await _eventRepo.GetByIdAsync(id, CancellationToken.None)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		_eventRepo.Remove(existingEvent);
		await _eventRepo.SaveChangesAsync(CancellationToken.None);
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