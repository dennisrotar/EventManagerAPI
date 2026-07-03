/// <summary>
/// Базовый класс для всех доменных исключений.
/// Не зависит от ASP.NET — статус-код указывается как int,
/// а не через StatusCodes из Microsoft.AspNetCore.Http.
/// Presentation-слой будет считывать StatusCode для формирования HTTP-ответа.
/// </summary>
public abstract class DomainException : Exception
{
	/// <summary>
	/// HTTP-статус код, соответствующий типу ошибки.
	/// Указывается как int (400, 404, 409 и т.д.), чтобы не зависеть от ASP.NET.
	/// </summary>
	public abstract int StatusCode { get; }

	/// <summary>
	/// Краткий заголовок ошибки (например, "Not Found", "Conflict").
	/// Используется в ProblemDetails.Title.
	/// </summary>
	public abstract string ErrorTitle { get; }

	/// <summary>
	/// URI с описанием типа ошибки по RFC 7807.
	/// Используется в ProblemDetails.Type.
	/// </summary>
	public abstract string ErrorType { get; }

	protected DomainException(string message) : base(message) { }
}