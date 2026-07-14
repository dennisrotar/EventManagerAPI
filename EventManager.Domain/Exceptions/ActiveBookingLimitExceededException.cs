namespace EventManager.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при превышении лимита активных бронирований одним пользователем.
/// </summary>
public class ActiveBookingLimitExceededException : DomainException
{
	public int Limit { get; }

	public ActiveBookingLimitExceededException(int limit) : base($"Превышен лимит активного бронирования ({limit}).")
	{
		Limit = limit;
	}

	/// <summary>HTTP 409 Conflict</summary>
	public override int StatusCode => 409;
	/// <summary>Заголовок ошибки</summary>
	public override string ErrorTitle => "Conflict";
	/// <summary>Ссылка на спецификацию RFC</summary>
	public override string ErrorType => "https://tools.ietf.org/html/rfc9110#section-15.5.10";
}