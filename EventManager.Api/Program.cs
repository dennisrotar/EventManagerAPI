using EventManagerAPI.DataAccess;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Подключаем Problem Details для красивых ошибок валидации
builder.Services.AddProblemDetails();

builder.Services.AddControllers();

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

// Регистрация сервиса в DI как Singleton (чтобы список событий не обнулялся)
builder.Services.AddSingleton<IEventService, EventService>();

// Регистрация хранилища (Singleton, так как данные в памяти)
builder.Services.AddSingleton<IBookingStore, InMemoryBookingStore>();

// Регистрация сервиса бронирований (Singleton, чтобы работал с Singleton-хранилищем)
builder.Services.AddSingleton<IBookingService, BookingService>();

// Регистрация фонового сервиса
builder.Services.AddHostedService<BookingBackgroundService>();

var app = builder.Build();

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
