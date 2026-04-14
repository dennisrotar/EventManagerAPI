using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventManagerAPI.Exceptions;

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
			Instance = httpContext.Request.Path
		};

		httpContext.Response.StatusCode = statusCode;
		httpContext.Response.ContentType = "application/problem+json";

		// Сериализуем и пишем ответ
		var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
		await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options), cancellationToken);

		// Возвращаем true, что означает "ошибка обработана нами, фреймворк ничего не делай"
		return true;
	}
}