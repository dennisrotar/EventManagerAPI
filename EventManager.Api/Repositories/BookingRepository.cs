using EventManagerAPI.DataAccess.Configurations;
using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagerAPI.Repositories;

public class BookingRepository : IBookingRepository
{
	private readonly AppDbContext _context;

	public BookingRepository(AppDbContext context) => _context = context;

	public async Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken ct) =>
		await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);

	public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct) =>
		await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);

	// Без AsNoTracking, чтобы EF Core отслеживал изменения для SaveChanges
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