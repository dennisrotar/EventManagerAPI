using System.ComponentModel.DataAnnotations;

namespace EventManager.Application.DTOs;

/// <summary>
/// DTO для запроса на обновление существующего мероприятия.
/// </summary>
public class UpdateEventRequestDto : IValidatableObject
{
	/// <summary>Название мероприятия. Не может быть пустым.</summary>
	[StringLength(int.MaxValue, MinimumLength = 1, ErrorMessage = "Название мероприятия не может быть пустым!")]
	public string Title { get; set; } = string.Empty;

	/// <summary>Описание мероприятия (опционально).</summary>
	public string? Description { get; set; }

	/// <summary>Дата и время начала.</summary>
	[Required(ErrorMessage = "Дата начала обязательна!")]
	public DateTime StartAt { get; set; }

	/// <summary>Дата и время окончания. Должна быть позже StartAt.</summary>
	[Required(ErrorMessage = "Дата окончания обязательна!")]
	public DateTime EndAt { get; set; }

	/// <summary>Количество мест. Должно быть больше 0.</summary>
	[Required(ErrorMessage = "Количество мест обязательно!")]
	[Range(1, int.MaxValue, ErrorMessage = "Количество мест должно быть больше 0!")]
	public int TotalSeats { get; set; }

	/// <summary>
	/// Кастомная валидация: EndAt > StartAt, StartAt не в прошлом.
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (EndAt <= StartAt)
		{
			yield return new ValidationResult(
				"Дата окончания должна быть позже даты начала!",
				new[] { nameof(EndAt) });
		}

		if (StartAt < DateTime.UtcNow)
		{
			yield return new ValidationResult(
				"Дата начала не может быть в прошлом!",
				new[] { nameof(StartAt) });
		}
	}
}