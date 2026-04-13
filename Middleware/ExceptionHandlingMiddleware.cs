using System.Text.Json;
using EventManagerAPI.Exceptions;

namespace EventManagerAPI.Middleware
{
	public class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<ExceptionHandlingMiddleware> _logger;

		public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Произошла непредвиденная ошибка: {Message}", ex.Message);
				await HandleExceptionAsync(context, ex);
			}
		}

		private static Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			context.Response.ContentType = "application/problem+json";

			var statusCode = exception switch
			{
				NotFoundException => StatusCodes.Status404NotFound,
				_ => StatusCodes.Status500InternalServerError
			};

			var problemDetails = new
			{
				type = $"https://tools.ietf.org/html/rfc7231#section-6.5.{statusCode}",
				title = GetTitle(statusCode),
				status = statusCode,
				detail = exception.Message,
				instance = context.Request.Path
			};

			context.Response.StatusCode = statusCode;
			return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			}));
		}

		private static string GetTitle(int statusCode) => statusCode switch
		{
			StatusCodes.Status404NotFound => "Not Found",
			StatusCodes.Status500InternalServerError => "Internal Server Error",
			_ => "An error occurred"
		};
	}
}
