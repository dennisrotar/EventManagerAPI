using EventManagerAPI.Models.Entities;

namespace EventManagerAPI.DataAccess;

/// <summary>
/// Интерфейс хранилища бронирований (In-memory).
/// </summary>
public interface IBookingStore
{
	void Add(Booking booking);
	Booking? GetById(Guid id);
	IEnumerable<Booking> GetPendingBookings();
	void Update(Booking booking);
}