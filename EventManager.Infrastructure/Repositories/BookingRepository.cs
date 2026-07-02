using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Infrastructure.Repositories;

/// <summary>
/// Реализация порта IBookingRepository через EF Core.
/// Адаптер: переводит вызовы Application в запросы к БД через DbContext.
/// Находится в Infrastructure, так как зависит от EF Core.
/// </summary>
public class BookingRepository : IBookingRepository
{
	private readonly AppDbContext _context;

	public BookingRepository(AppDbContext context) => _context = context;

	public async Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken ct) =>
		await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);

	/// <remarks>AsNoTracking — сущность не отслеживается EF Core, только для чтения.</remarks>
	public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct) =>
		await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);

	/// <remarks>FindAsync — сущность отслеживается EF Core, можно изменять и сохранять.</remarks>
	public async Task<Booking?> GetTrackedByIdAsync(Guid bookingId, CancellationToken ct) =>
		await _context.Bookings.FindAsync(bookingId, ct);

	public async Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken ct) =>
		await _context.Bookings
			.Where(b => b.Status == BookingStatus.Pending)
			.Select(b => b.Id)
			.ToListAsync(ct);

	public void Add(Booking booking) => _context.Bookings.Add(booking);

	public async Task SaveChangesAsync(CancellationToken ct) => await _context.SaveChangesAsync(ct);
}