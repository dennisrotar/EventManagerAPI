using EventManagerAPI.Models.DTOs;

namespace EventManagerAPI.Interfaces;

/// <summary>
/// Интерфейс сервиса для работы с мероприятиями.
/// </summary>
public interface IEventService
{
	/// <summary>
	/// Получает страницу мероприятий с применением фильтрации.
	/// </summary>
	/// <param name="query">Параметры фильтрации и пагинации.</param>
	/// <returns>DTO с результатом пагинации.</returns>
	PaginatedResultDto<EventResponseDto> GetFiltered(GetEventsQueryParams query);

	/// <summary>
	/// Получает информацию о мероприятии по ID.
	/// </summary>
	/// <param name="id">Идентификатор мероприятия.</param>
	/// <returns>DTO мероприятия.</returns>
	/// <exception cref="Exceptions.NotFoundException">Выбрасывается, если мероприятие не найдено.</exception>
	EventResponseDto GetById(Guid id);

	/// <summary>
	/// Создает новое мероприятие.
	/// </summary>
	/// <param name="dto">DTO с данными для создания.</param>
	/// <returns>DTO созданного мероприятия.</returns>
	EventResponseDto Create(CreateEventRequestDto dto);

	/// <summary>
	/// Обновляет существующее мероприятие.
	/// </summary>
	/// <param name="id">Идентификатор обновляемого мероприятия.</param>
	/// <param name="dto">DTO с новыми данными.</param>
	/// <exception cref="Exceptions.NotFoundException">Выбрасывается, если мероприятие не найдено.</exception>
	void Update(Guid id, UpdateEventRequestDto dto);

	/// <summary>
	/// Удаляет мероприятие.
	/// </summary>
	/// <param name="id">Идентификатор удаляемого мероприятия.</param>
	/// <exception cref="Exceptions.NotFoundException">Выбрасывается, если мероприятие не найдено.</exception>
	void Delete(Guid id);
}
