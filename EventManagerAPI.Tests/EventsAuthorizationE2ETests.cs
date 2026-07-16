using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EventManagerAPI.Tests;

public class EventsAuthorizationE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;
	private readonly HttpClient _client;

	public EventsAuthorizationE2ETests(WebApplicationFactory<Program> factory)
	{
		_factory = factory.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				// Полностью удаляем ВСЕ регистрации, связанные с AppDbContext
				var descriptors = services.Where(d =>
					d.ServiceType == typeof(AppDbContext) ||
					d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
					d.ServiceType == typeof(DbContextOptions) ||
					d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>)).ToList();

				foreach (var descriptor in descriptors)
				{
					services.Remove(descriptor);
				}

				// Добавляем чистый InMemory контекст
				services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestAuthDb"));
			});
		});

		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PostEvents_WithoutToken_Returns401Unauthorized()
	{
		// Arrange
		var dto = new CreateEventRequestDto
		{
			Title = "Test",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2),
			TotalSeats = 10
		};

		// Act
		var response = await _client.PostAsJsonAsync("/events", dto);

		// Assert
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task PostEvents_WithUserToken_Returns403Forbidden()
	{
		// Arrange
		// Генерируем реальный JWT-токен для роли User с помощью нашего сервиса
		using var scope = _factory.Services.CreateScope();
		var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
		var user = new User("testuser", "hash", Role.User);
		var token = tokenService.GenerateToken(user);

		// Добавляем токен в заголовок Authorization
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

		var dto = new CreateEventRequestDto
		{
			Title = "Test",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2),
			TotalSeats = 10
		};

		// Act
		var response = await _client.PostAsJsonAsync("/events", dto);

		// Assert
		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task PostEvents_WithAdminToken_Returns201Created()
	{
		// Arrange
		using var scope = _factory.Services.CreateScope();
		var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
		var admin = new User("adminuser", "hash", Role.Admin);
		var token = tokenService.GenerateToken(admin);

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

		var dto = new CreateEventRequestDto
		{
			Title = "Admin Event",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2),
			TotalSeats = 10
		};

		// Act
		var response = await _client.PostAsJsonAsync("/events", dto);

		// Assert
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}
}