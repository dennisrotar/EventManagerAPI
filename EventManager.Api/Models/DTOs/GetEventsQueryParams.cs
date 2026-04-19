using System.ComponentModel.DataAnnotations;

namespace EventManagerAPI.Models.DTOs
{
	/// <summary>
	/// DTO для параметров запроса: фильтрации и пагинации списка мероприятия.
	/// </summary>
	public class GetEventsQueryParams
	{
		/// <summary>
		/// Строка для поиска по названию (частичное совпадение, регистронезависимо).
		/// </summary>
		public string? Title { get; set; }

		/// <summary>
		/// Нижняя граница даты окончания мероприятия для фильтрации.
		/// </summary>
		public DateTime? From { get; set; }

		/// <summary>
		/// Верхняя граница даты окончания мероприятия для фильтрации.
		/// </summary>
		public DateTime? To { get; set; }

		/// <summary>
		/// Номер страница (по-умолчанию 1). Не м. б. меньше 1.
		/// </summary>
		[Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше или равен 1!")]
		public int Page { get; set; } = 1;

		/// <summary>
		/// Размер страницы (Количество элементов на странице, по-умолчанию 10). Не м. б. меньше 1.
		/// </summary>
		[Range(1, int.MaxValue, ErrorMessage = "Размер страницы должен быть больше или равен 1!")]
		public int PageSize { get; set; } = 10;
	}
}
