namespace EventManagerAPI.Exceptions;

/// <summary>
/// Исключение для ошибок валидации на уровне домена (внутри бизнес-логики).
/// </summary>
public class DomainValidationException : BaseApiException
{
	public DomainValidationException(string message) : base(message) { }

	public override int StatusCode => StatusCodes.Status400BadRequest;
	public override string Title => "Domain Validation Error";
	public override string Type => "https://tools.ietf.org/html/rfc9110#section-15.5.1";
}
