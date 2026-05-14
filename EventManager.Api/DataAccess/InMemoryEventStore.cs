using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.Entities;

namespace EventManagerAPI.DataAccess;

/// <summary>
/// Реализация in-memory хранилища мероприятий.
/// </summary>
public class InMemoryEventStore : IEventStore
{
	private readonly List<Event> _events = [];
	private readonly object _storeLock = new();

	public Event? GetById(Guid id) => _events.FirstOrDefault(e => e.Id == id);

	public void Add(Event eventEntity)
	{
		lock (_storeLock) { _events.Add(eventEntity); }
	}

	public void Update(Event eventEntity)
	{
		lock (_storeLock) { /* В памяти обновление свойств происходит по ссылке, но lock нужен для безопасности */ }
	}

	public void Remove(Event eventEntity)
	{
		lock (_storeLock) { _events.Remove(eventEntity); }
	}

	public (int TotalCount, List<Event> Items) GetFiltered(GetEventsQueryParams query)
	{
		var queryable = _events.AsQueryable();

		if (!string.IsNullOrWhiteSpace(query.Title))
			queryable = queryable.Where(e => e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));

		if (query.From.HasValue)
			queryable = queryable.Where(e => e.StartAt >= query.From.Value);

		if (query.To.HasValue)
			queryable = queryable.Where(e => e.EndAt <= query.To.Value);

		var totalCount = queryable.Count();

		var items = queryable.OrderBy(e => e.StartAt)
											.Skip((query.Page - 1) * query.PageSize)
											.Take(query.PageSize).ToList();

		return (totalCount, items);
	}
}