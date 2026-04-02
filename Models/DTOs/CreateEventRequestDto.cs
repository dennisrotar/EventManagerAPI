using System.ComponentModel.DataAnnotations;

namespace EventManagerAPI.Models.DTOs
{
	public class CreateEventRequestDto : IValidatableObject
	{
		[Required(ErrorMessage = "Название мероприятия обязательно!")]
		public string Title { get; set; } = string.Empty;

		public string? Description { get; set; }

		[Required(ErrorMessage = "Дата начала обязательна!")]
		public DateTime StartAt { get; set; }

		[Required(ErrorMessage = "Дата окончания обязательна!")]
		public DateTime EndAt { get; set; }

		// Кастомная валидация дат
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (EndAt <= StartAt)
			{
				yield return new ValidationResult(
					"Дата окончания должна быть позже даты начала!",
					new[] { nameof(EndAt) });
			}
		}
	}
}
