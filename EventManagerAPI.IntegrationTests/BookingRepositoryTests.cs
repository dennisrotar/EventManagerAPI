using EventManager.Domain.Entities;
using EventManagerAPI.IntegrationTests.Fixtures;
using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

[Collection("DatabaseTestCollection")]
public class BookingRepositoryTests : IntegrationTestBase
{
	public BookingRepositoryTests(PostgreSqlFixture fixture) : base(fixture) { }

	private async Task<Event> CreateTestEventInDb()
	{
		var eventEntity = Event.Create("Test Event", null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100);
		EventRepository.Add(eventEntity);
		await EventRepository.SaveChangesAsync(CancellationToken.None);
		return eventEntity;
	}

	[Fact]
	public async Task Add_ShouldPersistBookingToDatabase()
	{
		// Arrange
		var eventEntity = await CreateTestEventInDb();
		var booking = Booking.CreatePending(eventEntity.Id);

		// Act
		BookingRepository.Add(booking);
		await BookingRepository.SaveChangesAsync(CancellationToken.None);

		// Assert
		var savedBooking = await BookingRepository.GetByIdAsync(booking.Id, CancellationToken.None);
		Assert.NotNull(savedBooking);
		Assert.Equal(BookingStatus.Pending, savedBooking.Status);
		Assert.Equal(eventEntity.Id, savedBooking.EventId);
	}

	[Fact]
	public async Task GetById_WhenBookingExists_ShouldReturnBooking()
	{
		// Arrange
		var eventEntity = await CreateTestEventInDb();
		var booking = Booking.CreatePending(eventEntity.Id);
		BookingRepository.Add(booking);
		await BookingRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var foundBooking = await BookingRepository.GetByIdAsync(booking.Id, CancellationToken.None);

		// Assert
		Assert.NotNull(foundBooking);
		Assert.Equal(booking.Id, foundBooking.Id);
	}

	[Fact]
	public async Task GetTrackedById_WhenBookingExists_ShouldReturnTrackedEntity()
	{
		// Arrange
		var eventEntity = await CreateTestEventInDb();
		var booking = Booking.CreatePending(eventEntity.Id);
		BookingRepository.Add(booking);
		await BookingRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var trackedBooking = await BookingRepository.GetTrackedByIdAsync(booking.Id, CancellationToken.None);

		// Assert
		Assert.NotNull(trackedBooking);

		// Доказываем, что сущность отслеживается: меняем статус и сохраняем без явного вызова Update()
		trackedBooking.Confirm();
		await BookingRepository.SaveChangesAsync(CancellationToken.None);

		// Проверяем через не-отслеживаемый запрос, что статус реально изменился в БД
		var fromDb = await BookingRepository.GetByIdAsync(booking.Id, CancellationToken.None);
		Assert.Equal(BookingStatus.Confirmed, fromDb!.Status);
	}

	[Fact]
	public async Task GetEventById_WhenEventExists_ShouldReturnEvent()
	{
		// Arrange
		var eventEntity = await CreateTestEventInDb();

		// Act
		var foundEvent = await BookingRepository.GetEventByIdAsync(eventEntity.Id, CancellationToken.None);

		// Assert
		Assert.NotNull(foundEvent);
		Assert.Equal("Test Event", foundEvent.Title);
	}

	[Fact]
	public async Task GetPendingBookingIds_ShouldReturnOnlyPendingIds()
	{
		// Arrange
		var eventEntity = await CreateTestEventInDb();

		var booking1 = Booking.CreatePending(eventEntity.Id);
		var booking2 = Booking.CreatePending(eventEntity.Id);
		var booking3 = Booking.CreatePending(eventEntity.Id);

		booking2.Confirm(); // Меняем статус локально

		BookingRepository.Add(booking1);
		BookingRepository.Add(booking2);
		BookingRepository.Add(booking3);
		await BookingRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var pendingIds = await BookingRepository.GetPendingBookingIdsAsync(CancellationToken.None);

		// Assert
		Assert.Equal(2, pendingIds.Count);
		Assert.True(pendingIds.Contains(booking1.Id), "Список должен содержать первую бронь");
		Assert.True(pendingIds.Contains(booking3.Id), "Список должен содержать третью бронь");
		Assert.False(pendingIds.Contains(booking2.Id), "Список НЕ должен содержать подтвержденную бронь");
	}

	[Fact]
	public async Task AddBooking_ToNonExistentEvent_ShouldThrowDbUpdateException()
	{
		// Arrange
		var booking = Booking.CreatePending(Guid.NewGuid());
		BookingRepository.Add(booking);

		// Act & Assert
		await Assert.ThrowsAsync<DbUpdateException>(() => BookingRepository.SaveChangesAsync(CancellationToken.None));
	}

	[Fact]
	public async Task GetById_WhenBookingDoesNotExist_ShouldReturnNull()
	{
		// Arrange
		var nonExistentId = Guid.NewGuid();

		// Act
		var foundBooking = await BookingRepository.GetByIdAsync(nonExistentId, CancellationToken.None);

		// Assert
		Assert.Null(foundBooking);
	}
}