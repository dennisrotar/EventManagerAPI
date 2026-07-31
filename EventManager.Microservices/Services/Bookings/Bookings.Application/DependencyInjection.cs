using Bookings.Application.BackgroundServices;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Application;

/// <summary>
/// Extension-метод для регистрации всех сервисов Application-слоя.
/// Вызывается из Presentation (composition root в Program.cs).
/// НЕ регистрирует инфраструктурные зависимости (DbContext, репозитории) —
/// это делает AddInfrastructureServices() из Infrastructure-слоя.
/// </summary>
public static class DependencyInjection
{
	/// <summary>
	/// Регистрирует сервисы Application: use cases и фоновые сервисы.
	/// </summary>
	/// <param name="services">Коллекция сервисов DI-контейнера.</param>
	/// <returns>Та же коллекция для chaining.</returns>
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		services.AddScoped<IBookingService, BookingService>();
		services.AddHostedService<BookingBackgroundService>();
		return services;
	}
}