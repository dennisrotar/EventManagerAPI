namespace EventManager.Domain.Exceptions;

/// <summary>
/// Исключение для ошибок валидации на уровне домена.
/// Выбрасывается внутри бизнес-логики, когда нарушаются бизнес-правила.
/// Пример: количество мест на мероприятии <= 0.
/// </summary>
public class DomainValidationException : DomainException
{
	public DomainValidationException(string message) : base(message) { }

	/// <summary>HTTP 400 Bad Request</summary>
	public override int StatusCode => 400;
	/// <summary>Заголовок ошибки</summary>
	public override string ErrorTitle => "Domain Validation Error";
	/// <summary>Ссылка на спецификацию RFC</summary>
	public override string ErrorType => "https://tools.ietf.org/html/rfc9110#section-15.5.1";
}