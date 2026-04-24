using EventManagerAPI.Entities;

namespace EventManagerAPI.DataAccess;

/// <summary>
/// Реализация in-memory хранилища бронирований.
/// </summary>
public class InMemoryBookingStore : IBookingStore
{
	private readonly List<Booking> _bookings = new();

	public void Add(Booking booking) => _bookings.Add(booking);

	public Booking? GetById(Guid id) => _bookings.FirstOrDefault(b => b.Id == id);

	public IEnumerable<Booking> GetPendingBookings() => _bookings.Where(b => b.Status == BookingStatus.Pending).ToList();

	public void Update(Booking booking)
	{
		var index = _bookings.FindIndex(b => b.Id == booking.Id);
		if (index != -1) _bookings[index] = booking;
	}
}