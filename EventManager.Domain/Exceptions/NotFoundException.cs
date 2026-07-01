namespace EventManager.Domain.Exceptions;

/// <summary>
/// Исключение, указывающее на то, что запрашиваемый ресурс не найден.
/// Пример: мероприятие с указанным ID не существует.
/// </summary>
public class NotFoundException : DomainException
{
	public NotFoundException(string message) : base(message) { }

	/// <summary>HTTP 404 Not Found</summary>
	public override int StatusCode => 404;
	/// <summary>Заголовок ошибки</summary>
	public override string ErrorTitle => "Not Found";
	/// <summary>Ссылка на спецификацию RFC</summary>
	public override string ErrorType => "https://tools.ietf.org/html/rfc9110#section-15.5.5";
}