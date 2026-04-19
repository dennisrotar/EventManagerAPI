using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventManagerAPI.Exceptions;

/// <summary>
/// Глобальный обработчик исключений.
/// Отвечает СТРОГО за перехват необработанных бизнес-исключений (BaseApiException) 
/// и непредвиденных ошибок сервера (Exception).
/// Ошибки валидации (400) обрабатываются слоем выше (ApiBehaviorOptions в Program.cs) и сюда не попадают.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
	private readonly ILogger<GlobalExceptionHandler> _logger;

	public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
	{
		_logger = logger;
	}

	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		_logger.LogError(exception, "Произошла ошибка: {Message}", exception.Message);

		ProblemDetails problemDetails;

		// Проверяем, является ли ошибка нашей бизнес-ошибкой (наследником BaseApiException)
		if (exception is BaseApiException apiException)
		{
			// Берем статусы ПРЯМО из самого исключения (масштабируемость)
			problemDetails = new ProblemDetails
			{
				Status = apiException.StatusCode,
				Title = apiException.Title,
				Detail = apiException.Message,
				Type = apiException.Type,
				Instance = httpContext.Request.Path
			};
		}
		else
		{
			// Если это что-то непредвиденное (например, ошибка доступа к БД в будущем)
			problemDetails = new ProblemDetails
			{
				Status = StatusCodes.Status500InternalServerError,
				Title = "Internal Server Error",
				Detail = "Произошла непредвиденная ошибка на сервере.", // Для 500 не стоит светить детали exception.Message
				Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
				Instance = httpContext.Request.Path
			};
		}

		// Добавляем traceId для единообразия с ответами 400 от ASP.NET
		problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

		httpContext.Response.StatusCode = problemDetails.Status.Value;

		await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

		return true;
	}
}