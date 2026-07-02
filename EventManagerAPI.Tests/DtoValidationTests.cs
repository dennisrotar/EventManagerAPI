using EventManager.Application.DTOs;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EventManagerAPI.Tests;

/// <summary>
/// Юнит-тесты для проверки правил валидации DTO-моделей.
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

		// 1. Проверяем стандартные атрибуты
		Validator.TryValidateObject(model, context, results, validateAllProperties: true);

		// 2. Если модель реализует IValidatableObject, вызываем кастомную валидацию
		if (model is IValidatableObject validatableObject)
		{
			var customValidationResults = validatableObject.Validate(context);
			results.AddRange(customValidationResults);
		}

		return results;
	}

	[Fact]
	public void CreateEventDto_EmptyTitle_ShouldFailValidation()
	{
		var dto = new CreateEventRequestDto
		{
			Title = "",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2)
		};
		var errors = ValidateModel(dto);
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("не может быть пустым"));
	}

	[Fact]
	public void CreateEventDto_EndAtBeforeStartAt_ShouldFailValidation()
	{
		var dto = new CreateEventRequestDto
		{
			Title = "Тест",
			StartAt = DateTime.UtcNow.AddDays(2),
			EndAt = DateTime.UtcNow.AddDays(1)
		};
		var errors = ValidateModel(dto);
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("позже даты начала"));
	}

	[Fact]
	public void CreateEventDto_StartDateInPast_ShouldFailValidation()
	{
		var dto = new CreateEventRequestDto
		{
			Title = "Тест",
			StartAt = DateTime.UtcNow.AddDays(-1),
			EndAt = DateTime.UtcNow.AddDays(1)
		};
		var errors = ValidateModel(dto);
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("не может быть в прошлом"));
	}

	[Fact]
	public void UpdateEventDto_AllInvalidFields_ShouldFailValidation()
	{
		var dto = new UpdateEventRequestDto
		{
			Title = "",
			StartAt = DateTime.UtcNow.AddYears(-1),
			EndAt = DateTime.UtcNow.AddYears(-2),
			TotalSeats = 0
		};
		var errors = ValidateModel(dto);
		Assert.Equal(4, errors.Count);
	}

	[Fact]
	public void GetEventsQueryParams_PageLessThan1_ShouldFailValidation()
	{
		var query = new GetEventsQueryParams { Page = 0, PageSize = 10 };
		var errors = ValidateModel(query);
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("Номер страницы должен быть"));
	}

	[Fact]
	public void GetEventsQueryParams_PageSizeLessThan1_ShouldFailValidation()
	{
		var query = new GetEventsQueryParams { Page = 1, PageSize = -5 };
		var errors = ValidateModel(query);
		Assert.Contains(errors, e => e.ErrorMessage!.Contains("Размер страницы должен быть"));
	}

	[Fact]
	public void CreateEventDto_ValidData_ShouldPassValidation()
	{
		var dto = new CreateEventRequestDto
		{
			Title = "Валидное",
			StartAt = DateTime.UtcNow.AddMinutes(1),
			EndAt = DateTime.UtcNow.AddHours(1),
			TotalSeats = 10
		};
		var errors = ValidateModel(dto);
		Assert.Empty(errors);
	}
}