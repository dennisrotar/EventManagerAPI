using EventManagerAPI.Models;
using EventManagerAPI.Models.DTOs;

namespace EventManagerAPI.Interfaces
{
	/// <summary>
	/// Интерфейс сервиса для управления бизнес-логикой мероприятий.
	/// </summary>
	public interface IEventService
	{
		/// <summary>
		/// Получить все мероприятия.
		/// </summary>
		/// <returns> Весь список хранящихся мероприятий. </returns>
		List<Event> GetAll();

		/// <summary>
		/// Получить мероприятие по идентификатору.
		/// </summary>
		/// <param name="id"> Уникальный идентификатор мероприятия. </param>
		/// <returns> Модель меропрития. </returns>
		/// <exception cref="NotFoundException">Выбрасывается, если мероприятие не найдено.</exception>
		Event? GetById(Guid id);

		/// <summary>
		/// Создать новое мероприятие.
		/// </summary>
		/// <param name="dto">DTO с данными для создания.</param>
		/// <returns>Созданная модель мероприятия с сгенерированным Id.</returns>
		Event Create(CreateEventRequestDto dto);

		/// <summary>
		/// Обновить существующее мероприятие.
		/// </summary>
		/// <param name="id"> Идентификатор обновляемого мероприятия. </param>
		/// <param name="dto"> DTO с новыми данными. </param>
		/// <returns> Обновленная модель мероприятия. </returns>
		/// <exception cref="NotFoundException"> Выбрасывается, если мероприятие не найдено. </exception>
		Event? Update(Guid id, UpdateEventRequestDto dto);

		/// <summary>
		/// Удалить мероприятие по идентификатору.
		/// </summary>
		/// <param name="id"> Идентификатор удаляемого мероприятия. </param>
		/// <returns> True, если удаление успешно; False, если мероприятие не найдено. </returns>
		bool Delete(Guid id);

		/// <summary>
		/// Получить список мероприятий с применением фильтрации, сортировки и пагинации.
		/// </summary>
		/// <param name="query">Параметры фильтрации и пагинации.</param>
		/// <returns>DTO с полученным результатом.</returns>
		PaginatedResultDto<Event> GetFiltered(GetEventsQueryParams query);
	}
}
