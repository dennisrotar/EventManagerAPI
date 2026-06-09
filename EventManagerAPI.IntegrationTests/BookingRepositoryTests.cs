using EventManagerAPI.IntegrationTests.Fixtures;
using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

[Collection("DatabaseTestCollection")]
public class BookingRepositoryTests : IntegrationTestBase
{
	public BookingRepositoryTests(PostgreSqlFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public async Task AddBooking_ToNonExistentEvent_ShouldThrowDbUpdateException()
	{
		// Arrange
		var booking = Booking.CreatePending(Guid.NewGuid());
		BookingRepository.Add(booking);

		// Act & Assert
		// Проверяем, что внешним ключом из миграции БД не даст вставить бронь без события
		await Assert.ThrowsAsync<DbUpdateException>(() => BookingRepository.SaveChangesAsync(CancellationToken.None));
	}
}