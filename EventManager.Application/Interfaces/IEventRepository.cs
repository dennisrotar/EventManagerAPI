using EventManager.Domain.Entities;

namespace EventManager.Application.Interfaces;

/// <summary>
/// Порт (интерфейс) репозитория мероприятий.
/// Application определяет, какие данные ему нужны от инфраструктуры.
/// Конкретная реализация (EF Core, Dapper и т.д.) — в Infrastructure.
/// </summary>
public interface IEventRepository
{
	/// <summary>
	/// Получить отфильтрованный список мероприятий с пагинацией.
	/// </summary>
	/// <returns>Кортеж: список мероприятий и общее количество.</returns>
	Task<(List<Event> Items, int TotalCount)> GetFilteredAsync(
		string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);

	/// <summary>Получить мероприятие по ID. Null, если не найдено.</summary>
	Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);

	/// <summary>Добавить новое мероприятие в хранилище.</summary>
	void Add(Event eventEntity);

	/// <summary>Обновить существующее мероприятие в хранилище.</summary>
	void Update(Event eventEntity);

	/// <summary>Удалить мероприятие из хранилища.</summary>
	void Remove(Event eventEntity);

	/// <summary>Сохранить все изменения в хранилище.</summary>
	Task SaveChangesAsync(CancellationToken ct);
}