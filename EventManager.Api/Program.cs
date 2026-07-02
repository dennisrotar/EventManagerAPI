using EventManager.Application.Interfaces;
using EventManager.Infrastructure.DataAccess;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Repositories;
using EventManagerAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Подключаем Problem Details для красивых ошибок валидации
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		// Заставляем сериализатор возвращать enum'ы в виде строк ("Confirmed" вместо 1)
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

		// Явно добавляем traceId в расширения
		problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

		return new BadRequestObjectResult(problemDetails);
	};
});

// Подключаем наш, глобальный обработчик исключений.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация DbContext (Scoped)
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация Репозиториев (Scoped)
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

// Регистрация сервисов (Scoped)
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// Фоновый сервис (Singleton)
builder.Services.AddHostedService<BookingBackgroundService>();

var app = builder.Build();

// Применение миграций
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.Migrate(); // <-- Замена EnsureCreated()
}

// Включаем Swagger
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Включаем встроенный pipeline обработки исключений, 
// который теперь будет делегировать работу нашему GlobalExceptionHandler
app.UseExceptionHandler();
app.UseAuthorization();
app.MapControllers();

app.Run();
