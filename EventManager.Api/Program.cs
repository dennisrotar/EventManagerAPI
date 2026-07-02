using EventManager.Application;
using EventManager.Infrastructure;
using EventManager.Infrastructure.DataAccess;
using EventManagerAPI.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ════════════
// Framework Services
// ════════════

// Problem Details для красивых ошибок
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		// Enum'ы в виде строк ("Confirmed" вместо 1)
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});

// Настройка единого формата для ошибок 400 (валидация)
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.InvalidModelStateResponseFactory = context =>
	{
		var problemDetails = new ValidationProblemDetails(context.ModelState)
		{
			Status = 400,
			Title = "One or more validation errors occurred.",
			Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
			Instance = context.HttpContext.Request.Path
		};
		problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
		return new BadRequestObjectResult(problemDetails);
	};
});

// Глобальный обработчик исключений (маппит DomainException → HTTP)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ════════════════════════════════════
// Application Layer — бизнес-сервисы и фоновые сервисы
// ════════════════════════════════════
builder.Services.AddApplicationServices();

// ════════════════════════════════════════════
// Infrastructure Layer — DbContext, репозитории (реализации портов)
// ════════════════════════════════════════════
builder.Services.AddInfrastructureServices(builder.Configuration);

// ════
// Build
// ════
var app = builder.Build();

// Применение миграций при старте
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.Migrate();
}

// Swagger (только в Development)
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Включаем pipeline обработки исключений → делегирует GlobalExceptionHandler
app.UseExceptionHandler();
app.UseAuthorization();
app.MapControllers();

app.Run();