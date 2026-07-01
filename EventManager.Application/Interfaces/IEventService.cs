using EventManager.Application.DTOs;

namespace EventManager.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса (use case) для работы с мероприятиями.
/// Определяет бизнес-операции, которые можно выполнить над мероприятиями.
/// </summary>
public interface IEventService
{
	/// <summary>
	/// Получить страницу мероприятий с фильтрацией.
	/// </summary>
	/// <param name="query">Параметры фильтрации и пагинации.</param>
	/// <returns>DTO с результатом пагинации.</returns>
	Task<PaginatedResultDto<EventResponseDto>> GetFiltered(GetEventsQueryParams query);

	/// <summary>
	/// Получить мероприятие по ID.
	/// </summary>
	/// <exception cref="Domain.Exceptions.NotFoundException">
	/// Мероприятие не найдено.
	/// </exception>
	Task<EventResponseDto> GetById(Guid id);

	/// <summary>
	/// Создать новое мероприятие.
	/// </summary>
	/// <returns>DTO созданного мероприятия.</returns>
	Task<EventResponseDto> Create(CreateEventRequestDto dto);

	/// <summary>
	/// Обновить существующее мероприятие.
	/// </summary>
	/// <exception cref="Domain.Exceptions.NotFoundException">
	/// Мероприятие не найдено.
	/// </exception>
	Task Update(Guid id, UpdateEventRequestDto dto);

	/// <summary>
	/// Удалить мероприятие.
	/// </summary>
	/// <exception cref="Domain.Exceptions.NotFoundException">
	/// Мероприятие не найдено.
	/// </exception>
	Task Delete(Guid id);
}