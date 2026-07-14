namespace EventManager.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при попытке забронировать мероприятие,
/// дата начала которого уже прошла.
/// </summary>
public class PastEventBookingException : DomainException
{
	public PastEventBookingException(string message) : base(message) { }

	/// <summary>HTTP 400 Bad Request</summary>
	public override int StatusCode => 400;
	/// <summary>Заголовок ошибки</summary>
	public override string ErrorTitle => "Bad Request";
	/// <summary>Ссылка на спецификацию RFC</summary>
	public override string ErrorType => "https://tools.ietf.org/html/rfc9110#section-15.5.1";
}