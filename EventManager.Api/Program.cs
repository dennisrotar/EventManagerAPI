using EventManager.Application;
using EventManager.Infrastructure;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EventManagerAPI.Tests")]
[assembly: InternalsVisibleTo("EventManagerAPI.IntegrationTests")]

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Clean Architecture: Composition Root
// ──────────────────────────────────────────────

// Сервисы Application-слоя (DTO, интерфейсы-порты, бизнес-логика)
builder.Services.AddApplicationServices();

// Сервисы Infrastructure-слоя (EF Core, репозитории, БД)
builder.Services.AddInfrastructureServices(builder.Configuration);

// ──────────────────────────────────────────────
// Presentation-слой: контроллеры, Swagger, etc.
// ──────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ──────────────────────────────────────────────
// Pipeline
// ──────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ──────────────────────────────────────────────
// Для интеграционных тестов (WebApplicationFactory<Program>)
// ──────────────────────────────────────────────
public partial class Program { }