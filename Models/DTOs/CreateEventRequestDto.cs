using System.ComponentModel.DataAnnotations;

namespace EventManagerAPI.Models.DTOs
{
	/// <summary>
	/// DTO для запроса на создание нового мероприятия.
	/// </summary>
	public class CreateEventRequestDto : IValidatableObject
	{
		/// <summary>
		/// Название мероприятия. Не может быть пуустым.
		/// </summary>
		[StringLength(int.MaxValue, MinimumLength = 1, ErrorMessage = "Название мероприятия не может быть пустым!")]
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Описание мероприятия (опционально).
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Дата и время начала мероприятия. Не может быть в прошлом.
		/// </summary>
		[Required(ErrorMessage = "Дата начала обязательна!")]
		public DateTime StartAt { get; set; }

		/// <summary>
		/// Дата и время окончания мероприятия. Не может быть в прошлом.
		/// </summary>
		[Required(ErrorMessage = "Дата окончания обязательна!")]
		public DateTime EndAt { get; set; }

		/// <summary>
		/// Кастомная валидация логики дат.
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
}
