using EventManagerAPI.DataAccess.Configurations;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models.Entities;
using EventManagerAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventManagerAPI.Services;

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
				var bookingRepo = readScope.ServiceProvider.GetRequiredService<IBookingRepository>();
				pendingIds = await bookingRepo.GetPendingBookingIdsAsync(stoppingToken);
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
		var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

		try
		{
			await Task.Delay(ProcessingDelayMs, stoppingToken);

			var booking = await bookingRepo.GetTrackedByIdAsync(bookingId, stoppingToken);
			if (booking == null) return;

			var eventEntity = await bookingRepo.GetEventByIdAsync(booking.EventId, stoppingToken);
			if (eventEntity == null)
			{
				booking.Reject();
				await bookingRepo.SaveChangesAsync(stoppingToken);
				_logger.LogWarning("Событие удалено. Бронь {Id} отклонена", bookingId);
				return;
			}

			booking.Confirm();
			await bookingRepo.SaveChangesAsync(stoppingToken);
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