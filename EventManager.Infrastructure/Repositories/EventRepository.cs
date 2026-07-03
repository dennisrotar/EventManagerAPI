using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Infrastructure.Repositories;

/// <summary>
/// Реализация порта IEventRepository через EF Core.
/// Адаптер: переводит вызовы Application в запросы к БД через DbContext.
/// Находится в Infrastructure, так как зависит от EF Core.
/// </summary>
public class EventRepository : IEventRepository
{
	private readonly AppDbContext _context;

	public EventRepository(AppDbContext context) => _context = context;

	public async Task<(List<Event> Items, int TotalCount)> GetFilteredAsync(
		string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
	{
		var queryable = _context.Events.AsNoTracking().AsQueryable();

		// Фильтр по названию (ILike — регистронезависимый поиск в PostgreSQL)
		if (!string.IsNullOrWhiteSpace(title))
			queryable = queryable.Where(e => EF.Functions.ILike(e.Title, $"%{title}%"));

		// Фильтр по дате начала (от)
		if (from.HasValue)
			queryable = queryable.Where(e => e.StartAt >= from.Value);

		// Фильтр по дате окончания (до)
		if (to.HasValue)
			queryable = queryable.Where(e => e.EndAt <= to.Value);

		// Подсчёт общего количества (для пагинации)
		var totalCount = await queryable.CountAsync(ct);

		// Сортировка + пагинация
		var items = await queryable
			.OrderBy(e => e.StartAt)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);

		return (items, totalCount);
	}

	public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct) =>
		await _context.Events.FindAsync(id, ct);

	public void Add(Event eventEntity) => _context.Events.Add(eventEntity);

	public void Update(Event eventEntity) => _context.Events.Update(eventEntity);

	public void Remove(Event eventEntity) => _context.Events.Remove(eventEntity);

	public async Task SaveChangesAsync(CancellationToken ct) => await _context.SaveChangesAsync(ct);
}