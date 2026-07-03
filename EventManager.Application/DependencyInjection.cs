using EventManager.Application.BackgroundServices;
using EventManager.Application.Interfaces;
using EventManager.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Application;

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
		// Регистрация use case сервисов (Scoped — привязаны к HTTP-запросу)
		services.AddScoped<IEventService, EventService>();
		services.AddScoped<IBookingService, BookingService>();

		// Фоновый сервис обработки бронирований (Singleton — живёт всё время работы приложения)
		services.AddHostedService<BookingBackgroundService>();

		return services;
	}
}