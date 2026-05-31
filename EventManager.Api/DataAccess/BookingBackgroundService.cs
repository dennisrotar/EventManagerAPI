using EventManagerAPI.DataAccess;
using EventManagerAPI.DataAccess.Configurations;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventManagerAPI.DataAccess;

/// <summary>
/// Фоновый сервис для имитации отложенной обработки бронирований.
/// Опрашивает хранилище и переводит Pending брони в Confirmed.
/// </summary>
public class BookingBackgroundService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<BookingBackgroundService> _logger;

	private const int ProcessingDelayMs = 2000;
	private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(3);

	public BookingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BookingBackgroundService> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Фоновый сервис обработки бронирований запущен.");

		while (!stoppingToken.IsCancellationRequested)
		{
			List<Guid> pendingIds;

			// Создаем scope только для чтения ID (освобождаем быстро)
			using (var readScope = _scopeFactory.CreateScope())
			{
				var context = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
				pendingIds = context.Bookings
					.Where(b => b.Status == BookingStatus.Pending)
					.Select(b => b.Id)
					.ToList();
			}

			if (pendingIds.Any())
			{
				_logger.LogInformation("Найдено {Count} бронирований в статусе Pending", pendingIds.Count);

				// Для каждой брони создаем отдельный scope и задачу
				var tasks = pendingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
				await Task.WhenAll(tasks);
			}

			await Task.Delay(PollingInterval, stoppingToken);
		}
	}

	private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
	{
		_logger.LogDebug("Начало обработки брони {bookingId}", bookingId);

		// Создаем изолированный scope для обработки конкретной брони
		using var scope = _scopeFactory.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		try
		{
			await Task.Delay(ProcessingDelayMs, stoppingToken);

			var booking = await context.Bookings.FindAsync(bookingId);
			if (booking == null) return;

			var eventEntity = await context.Events.FindAsync(booking.EventId);
			if (eventEntity == null)
			{
				booking.Reject();
				await context.SaveChangesAsync();
				_logger.LogWarning("Событие удалено. Бронь {Id} отклонена", bookingId);
				return;
			}

			booking.Confirm();
			await context.SaveChangesAsync();
			_logger.LogInformation("Бронь {Id} подтверждена", bookingId);
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Обработка брони {bookingId} отменена", bookingId);
		}
		catch (Exception ex)
		{
			// Откат при непредвиденной ошибке: отклоняем бронь и возвращаем место
			_logger.LogError(ex, "Ошибка при обработке брони {bookingId}", bookingId);
		}
	}

}