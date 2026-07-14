namespace EventManager.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при попытке выполнить действие без достаточных прав.
/// </summary>
public class ForbiddenException : DomainException
{
	public ForbiddenException(string message = "Недостаточно прав на выполнение этого дейтсвия.") : base(message) { }

	/// <summary>HTTP 403 Forbidden</summary>
	public override int StatusCode => 403;
	/// <summary>Заголовок ошибки</summary>
	public override string ErrorTitle => "Forbidden";
	/// <summary>Ссылка на спецификацию RFC</summary>
	public override string ErrorType => "https://tools.ietf.org/html/rfc9110#section-15.5.4";
}