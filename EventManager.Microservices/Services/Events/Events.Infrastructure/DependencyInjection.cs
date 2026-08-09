using Events.Application.Interfaces;
using Events.Infrastructure.Cache;
using Events.Infrastructure.DataAccess;
using Events.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Events.Infrastructure;

/// <summary>
/// Extension-метод для регистрации всех Infrastructure-зависимостей в DI-контейнере.
/// Вызывается из Presentation (composition root в Program.cs).
/// </summary>
public static class DependencyInjection
{
	/// <summary>
	/// Регистрирует DbContext, репозитории и другие инфраструктурные сервисы.
	/// Принимает IConfiguration для чтения строки подключения.
	/// </summary>
	/// <param name="services">Коллекция сервисов DI-контейнера.</param>
	/// <param name="configuration">Конфигурация приложения (appsettings.json).</param>
	/// <returns>Та же коллекция для chaining.</returns>
	public static IServiceCollection AddInfrastructureServices(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

		services.AddScoped<IEventRepository, EventRepository>();

		// Регистрация Redis
		var redisConn = configuration.GetConnectionString("Redis")
						?? throw new InvalidOperationException("Redis connection string is missing");

		services.AddSingleton<IConnectionMultiplexer>(sp =>
			ConnectionMultiplexer.Connect(redisConn));

		services.AddScoped<ICacheService, RedisCacheService>();

		return services;
	}
}