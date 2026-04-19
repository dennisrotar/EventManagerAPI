using EventManagerAPI.Exceptions;
using EventManagerAPI.Models;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Services;
using Xunit;

namespace EventManagerAPI.Tests;

/// <summary>
/// Класс юнит-тестов для проверки бизнес-логики сервиса EventService.
/// </summary>
public class EventServiceTests
{
	private readonly EventService _service;

	public EventServiceTests()
	{
		_service = new EventService();
	}

	#region Успешные сценарии CRUD

	/// <summary>
	/// Проверяет, что сервис корректно создает мероприятие и присваивает ему Id.
	/// </summary>
	[Fact]
	public void Create_ShouldAddEventAndReturnIt()
	{
		// Arrange
		var dto = new CreateEventRequestDto
		{
			Title = "Тестовое событие",
			StartAt = DateTime.UtcNow,
			EndAt = DateTime.UtcNow.AddHours(2)
		};

		// Act
		var result = _service.Create(dto);

		// Assert
		Assert.NotNull(result);
		Assert.NotEqual(Guid.Empty, result.Id);
		Assert.Equal("Тестовое событие", result.Title);
	}

	/// <summary>
	/// Проверяет успешное получение существующего мероприятия по ID.
	/// </summary>
	[Fact]
	public void GetById_ShouldReturnEvent_WhenExists()
	{
		// Arrange
		var created = _service.Create(new CreateEventRequestDto { Title = "Найди меня", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) });

		// Act
		var result = _service.GetById(created.Id);

		// Assert
		Assert.Equal(created.Id, result.Id);
		Assert.Equal("Найди меня", result.Title);
	}

	/// <summary>
	/// Проверяет успешное обновление данных мероприятия.
	/// </summary>
	[Fact]
	public void Update_ShouldChangeEventData_WhenExists()
	{
		// Arrange
		var created = _service.Create(new CreateEventRequestDto { Title = "Старое", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) });
		var updateDto = new UpdateEventRequestDto { Title = "Новое", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(1).AddHours(1) };

		// Act
		Event? result = _service.Update(created.Id, updateDto);

		// Assert
		Assert.Equal("Новое", result.Title);
	}

	/// <summary>
	/// Проверяет успешное удаление существующего мероприятия.
	/// </summary>
	[Fact]
	public void Delete_ShouldRemoveEvent_WhenExists()
	{
		// Arrange
		var created = _service.Create(new CreateEventRequestDto { Title = "To Delete", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) });

		// Act
		_service.Delete(created.Id);

		// Assert
		// Проверяем, что после удаления сервис больше не может найти событие (кидает исключение)
		Assert.Throws<NotFoundException>(() => _service.GetById(created.Id));
	}

	#endregion

	#region Неуспешные сценарии

	/// <summary>
	/// Проверяет, что попытка получить несуществующее мероприятие выбрасывает NotFoundException.
	/// </summary>
	[Fact]
	public void GetById_ShouldThrowNotFoundException_WhenNotExists()
	{
		// Arrange
		var fakeId = Guid.NewGuid();

		// Act & Assert
		Assert.Throws<NotFoundException>(() => _service.GetById(fakeId));
	}

	/// <summary>
	/// Проверяет, что попытка обновить несуществующее мероприятие выбрасывает NotFoundException.
	/// </summary>
	[Fact]
	public void Update_ShouldThrowNotFoundException_WhenNotExists()
	{
		// Arrange
		var updateDto = new UpdateEventRequestDto { Title = "Обновление не найденного", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) };

		// Act & Assert
		Assert.Throws<NotFoundException>(() => _service.Update(Guid.NewGuid(), updateDto));
	}

	/// <summary>
	/// Проверяет, удаление несуществующего мероприятия, в случае если мероприятие не найдено, кидает исключение.
	/// </summary>
	[Fact]
	public void Delete_ShouldThrowNotFoundException_WhenNotExists()
	{
		// Arrange
		var fakeId = Guid.NewGuid();

		// Act & Assert
		Assert.Throws<NotFoundException>(() => _service.Delete(fakeId));
	}

	#endregion

	#region Фильтрация и Пагинация

	/// <summary>
	/// Проверяет корректность фильтрации по частичному совпадению названия (без учета регистра).
	/// </summary>
	[Fact]
	public void GetFiltered_FilterByTitle_ShouldReturnOnlyMatchingEvents()
	{
		// Arrange
		_service.Create(new CreateEventRequestDto { Title = "Баскетбол", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) });
		_service.Create(new CreateEventRequestDto { Title = "Учёба ЯП", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) });

		var query = new GetEventsQueryParams { Title = "баскет" }; // регистронезависимый

		// Act
		var result = _service.GetFiltered(query);

		// Assert
		Assert.Single(result.Items);
		Assert.Equal("Баскетбол", result.Items[0].Title);
	}

	/// <summary>
	/// Проверяет корректность фильтрации по диапазону дат.
	/// </summary>
	[Fact]
	public void GetFiltered_FilterByDates_ShouldReturnEventsInRange()
	{
		// Arrange
		_service.Create(new CreateEventRequestDto { Title = "Январь", StartAt = new DateTime(2024, 1, 10), EndAt = new DateTime(2024, 1, 11) });
		_service.Create(new CreateEventRequestDto { Title = "Февраль", StartAt = new DateTime(2024, 2, 10), EndAt = new DateTime(2024, 2, 11) });

		var query = new GetEventsQueryParams
		{
			From = new DateTime(2024, 1, 1),
			To = new DateTime(2024, 1, 31)
		};

		// Act
		var result = _service.GetFiltered(query);

		// Assert
		Assert.Single(result.Items);
		Assert.Equal("Январь", result.Items[0].Title);
	}

	/// <summary>
	/// Проверяет, что пагинация корректно отсчитывает пропущенные элементы (Skip/Take).
	/// </summary>
	[Fact]
	public void GetFiltered_Pagination_ShouldReturnCorrectPage()
	{
		// Arrange
		for (int i = 1; i <= 15; i++)
		{
			_service.Create(new CreateEventRequestDto { Title = $"Мероприятие {i}", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) });
		}

		var query = new GetEventsQueryParams { Page = 2, PageSize = 5 };

		// Act
		var result = _service.GetFiltered(query);

		// Assert
		Assert.Equal(15, result.TotalCount);
		Assert.Equal(5, result.Items.Count);
		Assert.Equal(2, result.Page);
	}

	/// <summary>
	/// Проверяет одновременное применение фильтра по названию и датам (логическое И).
	/// </summary>
	[Fact]
	public void GetFiltered_CombinedFiltering_ShouldWorkCorrectly()
	{
		// Arrange
		_service.Create(new CreateEventRequestDto { Title = "Дедлайн по проекту", StartAt = new DateTime(2024, 5, 10), EndAt = new DateTime(2024, 5, 11) });
		_service.Create(new CreateEventRequestDto { Title = "Дедлайн по домашке", StartAt = new DateTime(2024, 6, 10), EndAt = new DateTime(2024, 6, 11) });

		var query = new GetEventsQueryParams
		{
			Title = "Проект",
			From = new DateTime(2024, 5, 1),
			To = new DateTime(2024, 5, 31)
		};

		// Act
		var result = _service.GetFiltered(query);

		// Assert
		Assert.Single(result.Items);
		Assert.Equal("Дедлайн по проекту", result.Items[0].Title);
	}

	#endregion
}
