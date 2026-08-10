using Events.Application.DTOs;
using Events.Application.Interfaces;
using Events.Application.Services;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Events.Application.Tests;

public class EventServiceTests
{
	private readonly Mock<IEventRepository> _mockRepo;
	private readonly Mock<ICacheService> _mockCache;
	private readonly Mock<ILogger<EventService>> _mockLogger;
	private readonly EventService _service;

	public EventServiceTests()
	{
		_mockRepo = new Mock<IEventRepository>();
		_mockCache = new Mock<ICacheService>();
		_mockLogger = new Mock<ILogger<EventService>>();

		var settings = Options.Create(new CacheSettings
		{
			EventByIdTtl = TimeSpan.FromMinutes(10),
			TopEventsTtl = TimeSpan.FromMinutes(5)
		});

		_service = new EventService(_mockRepo.Object, _mockCache.Object, settings, _mockLogger.Object);
	}

	[Fact]
	public async Task GetById_CacheHit_DoesNotCallRepository()
	{
		// Arrange
		var eventId = Guid.NewGuid();
		var cachedDto = new EventResponseDto { Id = eventId, Title = "Cached Event" };

		_mockCache.Setup(c => c.GetAsync<EventResponseDto>($"event:{eventId}", It.IsAny<CancellationToken>()))
				  .ReturnsAsync(cachedDto);

		// Act
		var result = await _service.GetById(eventId);

		// Assert
		Assert.Equal(cachedDto, result);
		_mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task GetById_CacheMiss_CallsRepositoryAndSetsCache()
	{
		// Arrange
		var eventId = Guid.NewGuid();
		var eventEntity = Event.Create("Test Event", "Desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100);

		_mockCache.Setup(c => c.GetAsync<EventResponseDto>($"event:{eventId}", It.IsAny<CancellationToken>()))
				  .ReturnsAsync((EventResponseDto?)null);

		_mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
				 .ReturnsAsync(eventEntity);

		// Act
		var result = await _service.GetById(eventId);

		// Assert
		Assert.Equal(eventEntity.Id, result.Id);
		_mockRepo.Verify(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
		_mockCache.Verify(c => c.SetAsync($"event:{eventId}", It.IsAny<EventResponseDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Update_ValidData_InvalidatesCache()
	{
		// Arrange
		var eventId = Guid.NewGuid();
		var eventEntity = Event.Create("Old Title", "Desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100);
		var updateDto = new UpdateEventRequestDto { Title = "New Title", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(2), TotalSeats = 100 };

		_mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
				 .ReturnsAsync(eventEntity);

		// Act
		await _service.Update(eventId, updateDto);

		// Assert: Сначала БД, потом Кеш
		_mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		_mockCache.Verify(c => c.RemoveAsync($"event:{eventId}", It.IsAny<CancellationToken>()), Times.Once);
		_mockCache.Verify(c => c.RemoveAsync("events:top10", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Delete_ValidData_InvalidatesCache()
	{
		// Arrange
		var eventId = Guid.NewGuid();
		var eventEntity = Event.Create("Test Event", "Desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100);

		_mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
				 .ReturnsAsync(eventEntity);

		// Act
		await _service.Delete(eventId);

		// Assert
		_mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		_mockCache.Verify(c => c.RemoveAsync($"event:{eventId}", It.IsAny<CancellationToken>()), Times.Once);
	}
}