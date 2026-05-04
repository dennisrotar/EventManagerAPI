using EventManagerAPI.DataAccess;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Models.Entities;
using EventManagerAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

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
		// Передаем валидную заглушку логгера (NullLogger)
		_service = new EventService(_mockStore.Object, NullLogger<EventService>.Instance);
	}

	/// <summary>
	/// Вспомогательный метод для создания валидного DTO.
	/// Исключает дублирование кода в блоке Arrange тестов.
	/// </summary>
	/// <returns>Валидный объект CreateEventRequestDto с датами в будущем.</returns>
	private static CreateEventRequestDto GetValidDto() => new()
	{
		Title = "Тестище",
		StartAt = DateTime.UtcNow.AddHours(1),
		EndAt = DateTime.UtcNow.AddHours(2)
	};

	/// <summary>
	/// Проверяет, что метод Create корректно генерирует Id, 
	/// передает сущность в репозиторий и возвращает корректно замапленный DTO.
	/// </summary>
	[Fact]
	public void Create_ShouldAddEventAndReturnDto()
	{
		// Arrange
		var dto = GetValidDto();
		Event? captured = null;
		_mockStore.Setup(s => s.Add(It.IsAny<Event>())).Callback<Event>(e => captured = e);

		// Act
		var result = _service.Create(dto);

		// Assert
		Assert.NotNull(captured);
		Assert.NotEqual(Guid.Empty, captured.Id);
		Assert.Equal(dto.Title, result.Title);
		_mockStore.Verify(s => s.Add(It.IsAny<Event>()), Times.Once);
	}

	/// <summary>
	/// Проверяет, что метод GetById запрашивает сущность у репозитория 
	/// и возвращает корректный DTO, если сущность существует.
	/// </summary>
	[Fact]
	public void GetById_ShouldReturnEvent_WhenExists()
	{
		// Arrange
		var ev = new Event { Id = Guid.NewGuid(), Title = "Test" };
		_mockStore.Setup(s => s.GetById(ev.Id)).Returns(ev);

		// Act
		var result = _service.GetById(ev.Id);

		// Assert
		Assert.Equal(ev.Id, result.Id);
	}

	/// <summary>
	/// Проверяет, что метод GetById делегирует ответственность за отсутствие сущности 
	/// репозиторию и выбрасывает NotFoundException, если репозиторий вернул null.
	/// </summary>
	[Fact]
	public void GetById_ShouldThrow_WhenNotExists() =>
		Assert.Throws<NotFoundException>(() => _service.GetById(Guid.NewGuid()));

	/// <summary>
	/// Проверяет, что метод Update выбрасывает NotFoundException при попытке обновить несуществующую сущность.
	/// </summary>
	[Fact]
	public void Update_ShouldThrow_WhenNotExists() =>
		Assert.Throws<NotFoundException>(() => _service.Update(Guid.NewGuid(), new UpdateEventRequestDto { Title = "X", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) }));

	/// <summary>
	/// Проверяет, что метод Update успешно находит сущность, изменяет её свойства 
	/// и передает обновленную сущность в метод Update репозитория.
	/// </summary>
	[Fact]
	public void Update_ShouldPassUpdatedDataToStore_WhenExists()
	{
		// Arrange
		var eventId = Guid.NewGuid();
		var existingEvent = new Event { Id = eventId, Title = "Старое", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) };

		_mockStore.Setup(s => s.GetById(eventId)).Returns(existingEvent);

		Event? capturedEvent = null;
		_mockStore.Setup(s => s.Update(It.IsAny<Event>()))
				  .Callback<Event>(e => capturedEvent = e);

		var updateDto = new UpdateEventRequestDto
		{
			Title = "Новое",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(1).AddHours(1)
		};

		// Act
		_service.Update(eventId, updateDto);

		// Assert
		Assert.NotNull(capturedEvent);
		Assert.Equal("Новое", capturedEvent.Title);
		_mockStore.Verify(s => s.Update(It.IsAny<Event>()), Times.Once);
	}

	/// <summary>
	/// Проверяет, что метод Delete выбрасывает NotFoundException при попытке удалить несуществующую сущность.
	/// </summary>
	[Fact]
	public void Delete_ShouldThrow_WhenNotExists() =>
		Assert.Throws<NotFoundException>(() => _service.Delete(Guid.NewGuid()));

	/// <summary>
	/// Проверяет, что метод Delete успешно находит сущность и вызывает метод Remove в репозитории ровно один раз.
	/// </summary>
	[Fact]
	public void Delete_ShouldCallRemove_WhenExists()
	{
		// Arrange
		var ev = new Event { Id = Guid.NewGuid() };
		_mockStore.Setup(s => s.GetById(ev.Id)).Returns(ev);

		// Act
		_service.Delete(ev.Id);

		// Assert
		_mockStore.Verify(s => s.Remove(ev), Times.Once);
	}

	/// <summary>
	/// Проверяет, что метод GetFiltered делегирует ответственность за фильтрацию и пагинацию репозиторию,
	/// а затем корректно оборачивает результат в DTO пагинации.
	/// </summary>
	[Fact]
	public void GetFiltered_ShouldReturnPaginatedResult()
	{
		// Arrange
		var query = new GetEventsQueryParams { Page = 1, PageSize = 10 };
		_mockStore.Setup(s => s.GetFiltered(query)).Returns((1, new List<Event> { new() { Id = Guid.NewGuid() } }));

		// Act
		var result = _service.GetFiltered(query);

		// Assert
		Assert.Single(result.Items);
	}
}