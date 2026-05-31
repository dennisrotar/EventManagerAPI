using EventManagerAPI.DataAccess.Configurations;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagerAPI.Services;

/// <summary>
/// Реализация доменного сервиса мероприятий.
/// Зарегистрирована как Scoped, так как не имеет состояния и привязана к обработке конкретного запроса.
/// </summary>
public class EventService : IEventService
{
	private readonly AppDbContext _context;
	private readonly ILogger<EventService> _logger;

	/// <summary>
	/// Инициализирует новый экземпляр <see cref="EventService"/> с внедрением зависимостей.
	/// </summary>
	/// <param name="context">Репозиторий для доступа к данным.</param>
	/// <param name="logger">Логгер для записи отладочной информации.</param>
	public EventService(AppDbContext context, ILogger<EventService> logger)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<PaginatedResultDto<EventResponseDto>> GetFiltered(GetEventsQueryParams query)
	{
		_logger.LogInformation("Запрос списка событий");
		var queryable = _context.Events.AsNoTracking().AsQueryable();

		if (!string.IsNullOrWhiteSpace(query.Title))
			queryable = queryable.Where(e => e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
		if (query.From.HasValue)
			queryable = queryable.Where(e => e.StartAt >= query.From.Value);
		if (query.To.HasValue)
			queryable = queryable.Where(e => e.EndAt <= query.To.Value);

		var totalCount = await queryable.CountAsync();
		var items = await queryable.OrderBy(e => e.StartAt)
								.Skip((query.Page - 1) * query.PageSize)
								.Take(query.PageSize)
								.ToListAsync();

		return new PaginatedResultDto<EventResponseDto>
		{
			TotalCount = totalCount,
			Page = query.Page,
			PageSize = query.PageSize,
			Items = items.Select(MapToResponse).ToList()
		};
	}

	public async Task<EventResponseDto> GetById(Guid id)
	{
		_logger.LogDebug("Поиск события по ID: {Id}", id);

		var eventEntity = await _context.Events.FindAsync(id)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");
		return MapToResponse(eventEntity);
	}

	public async Task<EventResponseDto> Create(CreateEventRequestDto dto)
	{
		_logger.LogInformation("Создание нового события с заголовком: {Title}", dto.Title);

		var newEvent = Event.Create(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
		_context.Events.Add(newEvent);
		await _context.SaveChangesAsync();
		return MapToResponse(newEvent);
	}

	public async Task Update(Guid id, UpdateEventRequestDto dto)
	{
		_logger.LogInformation("Обновление события с ID: {Id}", id);

		var existingEvent = await _context.Events.FindAsync(id)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");

		existingEvent.UpdateDetails(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
		await _context.SaveChangesAsync();
	}

	public async Task Delete(Guid id)
	{
		_logger.LogInformation("Удаление события с ID: {Id}", id);

		var existingEvent = await _context.Events.FindAsync(id)
			?? throw new NotFoundException($"Мероприятие с ID {id} не найдено.");
		_context.Events.Remove(existingEvent);
		await _context.SaveChangesAsync();

		_logger.LogDebug("Событие с ID: {Id} успешно удалено", id);
	}

	/// <summary>
	/// Вспомогательный метод для маппинга доменной сущности в DTO ответа.
	/// </summary>
	/// <param name="e">Доменная сущность Event.</param>
	/// <returns>DTO для клиентского ответа.</returns>
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
