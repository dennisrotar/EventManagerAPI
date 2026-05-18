using EventManagerAPI.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EventManagerAPI.EventManagerAPI.Tests;

/// <summary>
/// Класс юнит-тестов для проверки правил валидации DTO-моделе.
/// </summary>
public class DtoValidationTests
{
	/// <summary>
	/// Вспомогательный метод для запуска валидации любой модели.
	/// </summary>
	private List<ValidationResult> ValidateModel(object model)
	{
		var context = new ValidationContext(model);
		var results = new List<ValidationResult>();

		// 1. Проверяем стандартные атрибуты ([Required], [StringLength], [Range] и т.д.)
		Validator.TryValidateObject(model, context, results, validateAllProperties: true);

		// 2. Если модель реализует IValidatableObject, явно вызываем её метод Validate
		// (чтобы собрать ошибки кастомной логики дат даже если были ошибки базовых атрибутов)
		if (model is IValidatableObject validatableObject)
		{
			var customValidationResults = validatableObject.Validate(context);
			results.AddRange(customValidationResults);
		}

		return results;
	}

	/// <summary>
	/// Проверяет, что DTO создания с пустым заголовком не проходит валидацию.
	/// </summary>
	[Fact]
	public void CreateEventDto_EmptyTitle_ShouldFailValidation()
	{
		// Arrange
		var dto = new CreateEventRequestDto
		{
			Title = "",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2)
		};

		// Act
		var errors = ValidateModel(dto);

		// Assert
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("не может быть пустым"));
	}

	/// <summary>
	/// Проверяет, что DTO создания с датой окончания раньше даты начала не проходит валидацию.
	/// </summary
	[Fact]
	public void CreateEventDto_EndAtBeforeStartAt_ShouldFailValidation()
	{
		// Arrange
		var dto = new CreateEventRequestDto
		{
			Title = "Тест",
			StartAt = DateTime.UtcNow.AddDays(2),
			EndAt = DateTime.UtcNow.AddDays(1)
		};

		// Act
		var errors = ValidateModel(dto);

		// Assert
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("позже даты начала"));
	}

	/// <summary>
	/// Проверяет, что DTO создания с датой начала в прошлом не проходит валидацию.
	/// </summary>
	[Fact]
	public void CreateEventDto_StartDateInPast_ShouldFailValidation()
	{
		// Arrange
		var dto = new CreateEventRequestDto
		{
			Title = "Тест",
			StartAt = DateTime.UtcNow.AddDays(-1), // В прошлом!
			EndAt = DateTime.UtcNow.AddDays(1)
		};

		// Act
		var errors = ValidateModel(dto);

		// Assert
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("не может быть в прошлом"));
	}

	/// <summary>
	/// Проверяет, что DTO обновления собирает все 3 ошибки при полностью невалидных данных.
	/// </summary>
	[Fact]
	public void UpdateEventDto_AllInvalidFields_ShouldFailValidation()
	{
		// Arrange
		var dto = new UpdateEventRequestDto
		{
			Title = "",
			StartAt = DateTime.UtcNow.AddYears(-1),
			EndAt = DateTime.UtcNow.AddYears(-2),
			TotalSeats = 0 // Добавили ошибку валидации мест
		};

		// Act
		var errors = ValidateModel(dto);

		// Assert - теперь ожидаем 4 ошибки (пустой титл, прошлое, даты и места)
		Assert.Equal(4, errors.Count);
	}

	/// <summary>
	/// Проверяет, что параметры пагинации с номером страницы меньше 1 не проходят валидацию.
	/// </summary>
	[Fact]
	public void GetEventsQueryParams_PageLessThan1_ShouldFailValidation()
	{
		// Arrange
		var query = new GetEventsQueryParams { Page = 0, PageSize = 10 };

		// Act
		var errors = ValidateModel(query);

		// Assert
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("Номер страницы должен быть"));
	}

	/// <summary>
	/// Проверяет, что параметры пагинации с размером страницы меньше 1 не проходят валидацию.
	/// </summary>
	[Fact]
	public void GetEventsQueryParams_PageSizeLessThan1_ShouldFailValidation()
	{
		// Arrange
		var query = new GetEventsQueryParams { Page = 1, PageSize = -5 };

		// Act
		var errors = ValidateModel(query);

		// Assert
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("Размер страницы должен быть"));
	}

	/// <summary>
	/// Проверяет, что полностью валидное DTO создания не возвращает ошибок валидации.
	/// </summary>
	[Fact]
	public void CreateEventDto_ValidData_ShouldPassValidation()
	{
		// Arrange
		var dto = new CreateEventRequestDto
		{
			Title = "Валидное",
			StartAt = DateTime.UtcNow.AddMinutes(1),
			EndAt = DateTime.UtcNow.AddHours(1),
			TotalSeats = 10 // Добавили валидное количество мест
		};

		// Act
		var errors = ValidateModel(dto);

		// Assert
		Assert.Empty(errors);
	}
}