namespace EventManagerAPI.Exceptions
{
	/// <summary>
	/// Кастомное исключение, указывающее на то, что запрашиваемый ресурс не найден.
	/// </summary>
	public class NotFoundException : BaseApiException
	{
		public NotFoundException(string message) : base(message) { }

		public override int StatusCode => StatusCodes.Status404NotFound;
		public override string Title => "Not Found";
		public override string Type => "https://tools.ietf.org/html/rfc9110#section-15.5.5";
	}
}
