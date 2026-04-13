using EventManagerAPI.Interfaces;
using EventManagerAPI.Middleware;
using EventManagerAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Подключаем Problem Details для красивых ошибок валидации
builder.Services.AddProblemDetails();

// Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация сервиса в DI как Singleton (чтобы список событий не обнулялся)
builder.Services.AddSingleton<IEventService, EventService>();

var app = builder.Build();

// Включаем Swagger
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Регистрация Middleware
app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

app.Run();
