namespace EventManagerAPI.Exceptions
{
	/// <summary>
	/// Базовый класс для всех пользовательских исключений API.
	/// Позволяет исключению самому определять свой HTTP-статус и метаданные RFC 7807,
	/// избавляя GlobalExceptionHandler от жестких привязок (if/switch) к конкретным типам ошибок.
	/// </summary>
	public abstract class BaseApiException : Exception
	{
		public abstract int StatusCode { get; }
		public abstract string Title { get; }
		public abstract string Type { get; }

		protected BaseApiException(string message) : base(message) { }
	}
}
