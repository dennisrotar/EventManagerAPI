using EventManager.Application;
using EventManager.Infrastructure;
using EventManager.Infrastructure.DataAccess;
using EventManagerAPI.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════
// Framework Services
// ═══════════════════════════════════════════════════

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

// Настройка JWT Аутентификации
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = jwtSettings["Issuer"],
			ValidateAudience = true,
			ValidAudience = jwtSettings["Audience"],
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(key)
		};
	});

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "EventManagerAPI", Version = "v1" });

	// Настройка кнопки Authorize в Swagger для передачи JWT-токена
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer"
	});
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

// ═══════════════════════════════════════════════════
// Application Layer — бизнес-сервисы и фоновые сервисы
// ═══════════════════════════════════════════════════
builder.Services.AddApplicationServices();

// ═══════════════════════════════════════════════════
// Infrastructure Layer — DbContext, репозитории (реализации портов)
// ═══════════════════════════════════════════════════
builder.Services.AddInfrastructureServices(builder.Configuration);

// ═══════════════════════════════════════════════════
// Build
// ═══════════════════════════════════════════════════
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

// ВАЖНО: Сначала Authentication, затем Authorization!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();