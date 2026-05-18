using EventManagerAPI.DataAccess;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.Entities;
using EventManagerAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EventManager.Api.Tests;

/// <summary>
/// Класс юнит-тестов для доменного сервиса EventService.
/// Использует фреймворк Moq для полной изоляции тестируемой логики 
/// от инфраструктурного слоя (репозитория IEventStore).
/// </summary>
public class EventServiceTests
{
	private readonly Mock<IEventStore> _mockStore;
	private readonly EventService _service;

	public EventServiceTests()
	{
		_mockStore = new Mock<IEventStore>();
		_service = new EventService(_mockStore.Object, NullLogger<EventService>.Instance);
	}

	/// <summary>
	/// Вспомогательный метод для создания валидного DTO.
	/// </summary>
	private static CreateEventRequestDto GetValidDto() => new()
	{
		Title = "Тест",
		StartAt = DateTime.UtcNow.AddHours(1),
		EndAt = DateTime.UtcNow.AddHours(2),
		TotalSeats = 10
	};

	[Fact]
	public void Create_ShouldAddEventAndReturnDto()
	{
		var dto = GetValidDto();
		Event? captured = null;
		_mockStore.Setup(s => s.Add(It.IsAny<Event>())).Callback<Event>(e => captured = e);

		var result = _service.Create(dto);

		Assert.NotNull(captured);
		Assert.NotEqual(Guid.Empty, captured.Id);
		Assert.Equal(dto.Title, result.Title);
		_mockStore.Verify(s => s.Add(It.IsAny<Event>()), Times.Once);
	}

	[Fact]
	public void GetById_ShouldReturnEvent_WhenExists()
	{
		// Используем фабрику Event.Create ВМЕСТО new Event
		var ev = Event.Create("Test", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
		_mockStore.Setup(s => s.GetById(ev.Id)).Returns(ev);

		var result = _service.GetById(ev.Id);
		Assert.Equal(ev.Id, result.Id);
	}

	[Fact]
	public void GetById_ShouldThrow_WhenNotExists() =>
		Assert.Throws<NotFoundException>(() => _service.GetById(Guid.NewGuid()));

	[Fact]
	public void Update_ShouldThrow_WhenNotExists() =>
		Assert.Throws<NotFoundException>(() => _service.Update(Guid.NewGuid(), new UpdateEventRequestDto { Title = "X", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10 }));

	[Fact]
	public void Update_ShouldPassUpdatedDataToStore_WhenExists()
	{
		var eventId = Guid.NewGuid();
		// Используем фабрику
		var existingEvent = Event.Create("Старое", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

		// Хак для теста: так как мы мокаем хранилище, мы подменяем Id напрямую 
		// (в реальном коде Id генерируется внутри фабрики, а в тесте нам нужен конкретный)
		var type = typeof(Event);
		type.GetProperty("Id")!.SetValue(existingEvent, eventId);

		_mockStore.Setup(s => s.GetById(eventId)).Returns(existingEvent);

		Event? capturedEvent = null;
		_mockStore.Setup(s => s.Update(It.IsAny<Event>()))
				  .Callback<Event>(e => capturedEvent = e);

		var updateDto = new UpdateEventRequestDto
		{
			Title = "Новое",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(1).AddHours(1),
			TotalSeats = 10
		};

		_service.Update(eventId, updateDto);

		Assert.NotNull(capturedEvent);
		Assert.Equal("Новое", capturedEvent.Title);
		_mockStore.Verify(s => s.Update(It.IsAny<Event>()), Times.Once);
	}

	[Fact]
	public void Delete_ShouldThrow_WhenNotExists() =>
		Assert.Throws<NotFoundException>(() => _service.Delete(Guid.NewGuid()));

	[Fact]
	public void Delete_ShouldCallRemove_WhenExists()
	{
		// Используем фабрику
		var ev = Event.Create("Test", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
		_mockStore.Setup(s => s.GetById(ev.Id)).Returns(ev);

		_service.Delete(ev.Id);

		_mockStore.Verify(s => s.Remove(ev), Times.Once);
	}

	[Fact]
	public void GetFiltered_ShouldReturnPaginatedResult()
	{
		var query = new GetEventsQueryParams { Page = 1, PageSize = 10 };
		// Используем фабрику внутри List
		_mockStore.Setup(s => s.GetFiltered(query)).Returns((1, new List<Event> { Event.Create("T", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10) }));

		var result = _service.GetFiltered(query);

		Assert.Single(result.Items);
	}
}