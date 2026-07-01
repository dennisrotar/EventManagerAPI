using EventManager.Domain.Entities;
using EventManagerAPI.Models.Entities;

namespace EventManagerAPI.Repositories;

public interface IBookingRepository
{
	Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken ct);
	Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct);
	Task<Booking?> GetTrackedByIdAsync(Guid bookingId, CancellationToken ct);
	Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken ct);
	void Add(Booking booking);
	Task SaveChangesAsync(CancellationToken ct);
}