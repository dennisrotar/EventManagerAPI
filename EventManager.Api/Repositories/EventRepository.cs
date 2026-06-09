using EventManagerAPI.DataAccess.Configurations;
using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagerAPI.Repositories;

public class EventRepository : IEventRepository
{
	private readonly AppDbContext _context;

	public EventRepository(AppDbContext context) => _context = context;

	public async Task<(List<Event> Items, int TotalCount)> GetFilteredAsync(
		string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
	{
		var queryable = _context.Events.AsNoTracking().AsQueryable();

		if (!string.IsNullOrWhiteSpace(title))
			queryable = queryable.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
		if (from.HasValue)
			queryable = queryable.Where(e => e.StartAt >= from.Value);
		if (to.HasValue)
			queryable = queryable.Where(e => e.EndAt <= to.Value);

		var totalCount = await queryable.CountAsync(ct);
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