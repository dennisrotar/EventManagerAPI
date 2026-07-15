using EventManager.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventManagerAPI.Exceptions;

/// <summary>
/// Глобальный обработчик исключений.
/// Перехватывает доменные исключения (DomainException) и маппит их в HTTP-ответы.
/// Ошибки валидации (400) обрабатываются слоем выше (ApiBehaviorOptions в Program.cs)
/// и сюда не попадают.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
	private readonly ILogger<GlobalExceptionHandler> _logger;

	public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Пытается обработать исключение.
	/// </summary>
	/// <returns>True — исключение обработано; False — передать следующему обработчику.</returns>
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		_logger.LogError(exception, "Произошла ошибка: {Message}", exception.Message);

		ProblemDetails problemDetails;

		// Проверяем: является ли ошибка доменной (наследником DomainException)
		if (exception is DomainException domainException)
		{
			// Берём данные из самого исключения (StatusCode — int, ErrorTitle, ErrorType)
			problemDetails = new ProblemDetails
			{
				Status = domainException.StatusCode,
				Title = domainException.ErrorTitle,
				Detail = domainException.Message,
				Type = domainException.ErrorType,
				Instance = httpContext.Request.Path
			};
		}
		else
		{
			// Непредвиденная ошибка (например, ошибка доступа к БД)
			problemDetails = new ProblemDetails
			{
				Status = StatusCodes.Status500InternalServerError,
				Title = "Internal Server Error",
				Detail = "Произошла непредвиденная ошибка на сервере.",
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