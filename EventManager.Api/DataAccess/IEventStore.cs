using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.Entities;

namespace EventManagerAPI.DataAccess;

/// <summary>
/// Интерфейс хранилища мероприятий (In-memory).
/// </summary>
public interface IEventStore
{
	/// <summary>
	/// Получает сущность мероприятия по идентификатору.
	/// </summary>
	/// <param name="id">Уникальный идентификатор мероприятия.</param>
	/// <returns>Сущность Event или null, если не найдена.</returns>
	Event? GetById(Guid id);

	/// <summary>
	/// Добавляет новую сущность мероприятия в хранилище.
	/// </summary>
	/// <param name="eventEntity">Добавляемая сущность.</param>
	void Add(Event eventEntity);

	/// <summary>
	/// Обновляет существующую сущность в хранилище.
	/// </summary>
	/// <param name="eventEntity">Обновляемая сущность.</param>
	void Update(Event eventEntity);

	/// <summary>
	/// Удаляет сущность мероприятия из хранилища.
	/// </summary>
	/// <param name="eventEntity">Удаляемая сущность.</param>
	void Remove(Event eventEntity);

	/// <summary>
	/// Получает отфильтрованный и разбитый на страницы список сырых доменных сущностей.
	/// Фильтрация и пагинация являются ответственностью репозитория.
	/// </summary>
	/// <param name="query">Параметры фильтрации и пагинации.</param>
	/// <returns>Кортеж: общее количество подходящих элементов и список элементов для текущей страницы.</returns>
	(int TotalCount, List<Event> Items) GetFiltered(GetEventsQueryParams query);
}
