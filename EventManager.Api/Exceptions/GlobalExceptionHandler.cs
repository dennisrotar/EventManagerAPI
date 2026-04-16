using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventManagerAPI.Exceptions;

/// <summary>
/// Глобальный обработчик исключений.
/// Перехватывает все неперехваченные исключения и формирует единый Json-ответ по стандарту RFC 7807 (ProblemDetails).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
	private readonly ILogger<GlobalExceptionHandler> _logger;

	public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Основной метод обработчки исключения.
	/// Вызывается фреймворком автоматически.
	/// </summary>
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		// Логируем ошибку
		_logger.LogError(exception, "Произошла ошибка: {Message}", exception.Message);

		// Определяем статус код
		var statusCode = exception switch
		{
			NotFoundException => StatusCodes.Status404NotFound,
			_ => StatusCodes.Status500InternalServerError
		};

		// Формируем единый ответ (используем встроенный класс ProblemDetails)
		var problemDetails = new ProblemDetails
		{
			Status = statusCode,
			Title = statusCode == 404 ? "Not Found" : "Internal Server Error",
			Detail = exception.Message,
			Instance = httpContext.Request.Path,
			Type = statusCode == 404
						? "https://tools.ietf.org/html/rfc9110#section-15.5.5"
						: "https://tools.ietf.org/html/rfc9110#section-15.6.1"
		};

		problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

		httpContext.Response.StatusCode = statusCode;
		// WriteAsJsonAsync автоматически добавит нужные заголовки и сериализует объект в точно таком же формате,
		// как это делает встроеный валидатор.
		await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

		return true;
	}
}