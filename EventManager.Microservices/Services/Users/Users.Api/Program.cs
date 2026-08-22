using Users.Application;
using Users.Infrastructure;
using Users.Infrastructure.DataAccess;
using Users.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════
// Serilog (Структурированное логирование в JSON)
// ═══════════════════════════════════════════════════
builder.Host.UseSerilog((ctx, cfg) =>
	cfg.ReadFrom.Configuration(ctx.Configuration)
	   .WriteTo.Console(new CompactJsonFormatter()));

// ═══════════════════════════════════════════════════
// OpenTelemetry (Traces + Metrics)
// ═══════════════════════════════════════════════════
builder.Services.AddOpenTelemetry()
	.ConfigureResource(r => r.AddService(
		serviceName: builder.Configuration["ServiceName"]!))
	.WithTracing(tracing => tracing
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddEntityFrameworkCoreInstrumentation()
		.AddOtlpExporter(o => o.Endpoint =
			new Uri(builder.Configuration["Otlp:Endpoint"]!)))
	.WithMetrics(metrics => metrics
		.AddAspNetCoreInstrumentation()
		.AddRuntimeInstrumentation()
		.AddPrometheusExporter());

// ═══════════════════════════════════════════════════
// Framework Services
// ═══════════════════════════════════════════════════
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});

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

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "Users API", Version = "v1" });
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT"
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
// Application & Infrastructure Layers
// ═══════════════════════════════════════════════════
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ═══════════════════════════════════════════════════
// Build
// ═══════════════════════════════════════════════════
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
	{
		db.Database.Migrate();
	}
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Эндпоинт для скрейпинга метрик Prometheus
app.MapPrometheusScrapingEndpoint();

app.Run();

public partial class Program { }