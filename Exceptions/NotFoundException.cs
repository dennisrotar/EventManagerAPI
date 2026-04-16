namespace EventManagerAPI.Exceptions
{
	/// <summary>
	/// Кастомное исключение, указывающее на то, что запрашиваемый ресурс не найден.
	/// </summary>
	public class NotFoundException : Exception
	{
		/// <summary>
		/// Инициализирует новый экземпляр исключения с заданным сообщением об ошибке.
		/// </summary>
		/// <param name="message"> Сообщние об ошибке для клинета. </param>
		public NotFoundException(string message) : base(message) { } 
	}
}
