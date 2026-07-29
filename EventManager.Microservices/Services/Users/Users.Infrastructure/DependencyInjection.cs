using Users.Application.Interfaces;
using Users.Infrastructure.DataAccess;
using Users.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Infrastructure.Security;

namespace Users.Infrastructure;

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

		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IPasswordHasher, PasswordHasher>();
		services.AddScoped<ITokenService, JwtTokenService>();

		return services;
	}
}