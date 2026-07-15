using EventManager.Application.Interfaces;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventManager.Infrastructure.Security;

namespace EventManager.Infrastructure;

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
		// Регистрация DbContext с PostgreSQL провайдером (Scoped — по одному на HTTP-запрос)
		services.AddDbContext<AppDbContext>(options =>
		options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
			b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

		// Регистрация реализаций портов (адаптеров) — Scoped
		services.AddScoped<IEventRepository, EventRepository>();
		services.AddScoped<IBookingRepository, BookingRepository>();
		services.AddScoped<IUserRepository, UserRepository>();

		// Регистрация сервисов безопасности (хеширование паролей и генерация JWT)
		// PasswordHasher не имеет состояния, поэтому Singleton (или Scoped)
		services.AddSingleton<IPasswordHasher, PasswordHasher>();
		// JwtTokenService читает конфигурацию, регистрируем как Scoped
		services.AddScoped<ITokenService, JwtTokenService>();

		return services;
	}
}