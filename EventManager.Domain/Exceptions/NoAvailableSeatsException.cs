namespace EventManager.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при попытке забронировать место,
/// когда свободных мест на мероприятии нет.
/// </summary>
public class NoAvailableSeatsException : DomainException
{
	public NoAvailableSeatsException(string message) : base(message) { }

	/// <summary>HTTP 409 Conflict</summary>
	public override int StatusCode => 409;
	/// <summary>Заголовок ошибки</summary>
	public override string ErrorTitle => "Conflict";
	/// <summary>Ссылка на спецификацию RFC</summary>
	public override string ErrorType => "https://tools.ietf.org/html/rfc9110#section-15.5.10";
}