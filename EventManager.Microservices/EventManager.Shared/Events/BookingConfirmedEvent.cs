using System;

namespace EventManager.Shared.Events
{
	public record BookingConfirmedEvent(
		Guid BookingId,
		Guid EventId,
		Guid UserId,
		int SeatsBooked,
		DateTime ConfirmedAt
	);
}