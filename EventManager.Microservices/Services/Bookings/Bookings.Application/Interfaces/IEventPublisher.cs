using EventManager.Shared.Events;

namespace Bookings.Application.Interfaces
{
	public interface IEventPublisher
	{
		Task PublishBookingConfirmedAsync(BookingConfirmedEvent @event, Guid eventId);
	}
}