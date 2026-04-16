using Microsoft.OpenApi;

namespace EventManagerAPI.Models.DTOs
{
	/// <summary>
	/// Обобщенный DTO для возврата результата с пагинацией.
	/// </summary>
	/// <typeparam name="T"> Тип элементов в коллекции. </typeparam>
	public class PaginatedResultDto<T>
	{
		/// <summary>
		/// Общее количество элементов, подходящих под фильтр (без учета пагинации).
		/// </summary>
		public int TotalCount { get; set; }

		/// <summary>
		/// Текущий запрошенный номер страницы.
		/// </summary>
		public int Page { get; set; }

		/// <summary>
		/// Запрошенный размер страницы.
		/// </summary>
		public int PageSize { get; set; }

		/// <summary>
		/// Коллекция элементов на текущей странице.
		/// </summary>
		public List<T> Items { get; set; } = new();
	}
}
