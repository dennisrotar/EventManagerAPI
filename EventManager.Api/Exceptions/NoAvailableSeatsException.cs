namespace EventManagerAPI.Exceptions
{
	public class NoAvailableSeatsException : BaseApiException
	{
		public NoAvailableSeatsException(string message) : base(message) { }

		public override int StatusCode => StatusCodes.Status409Conflict;
		public override string Title => "Conflict";
		public override string Type => "https://tools.ietf.org/html/rfc9110#section-15.5.10";
	}
}
