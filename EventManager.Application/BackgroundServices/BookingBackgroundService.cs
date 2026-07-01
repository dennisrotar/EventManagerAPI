using EventManager.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventManager.Application.BackgroundServices;

/// <summary>
/// Фоновый сервис для имитации отложенной обработки бронирований.
/// Опрашивает хранилище через порт IBookingRepository и переводит
/// Pending-брони в Confirmed.
/// Отнесён к Application, так как оркестрирует бизнес-процесс.
/// </summary>
public class BookingBackgroundService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<BookingBackgroundService> _logger;

	/// <summary>Задержка перед подтверждением брони (имитация обработки).</summary>
	private const int ProcessingDelayMs = 2000;

	/// <summary>Интервал опроса Pending-броней.</summary>
	private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(3);

	public BookingBackgroundService(
		IServiceScopeFactory scopeFactory,
		ILogger<BookingBackgroundService> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	/// <summary>
	/// Основной цикл фоновой обработки.
	/// Каждые 3 секунды проверяет наличие Pending-бронирований и подтверждает их.
	/// </summary>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Фоновый сервис обработки бронирований запущен.");

		while (!stoppingToken.IsCancellationRequested)
		{
			List<Guid> pendingIds;

			// Создаём scope только для чтения ID (освобождаем быстро)
			using (var readScope = _scopeFactory.CreateScope())
			{
				var bookingRepo = readScope.ServiceProvider.GetRequiredService<IBookingRepository>();
				pendingIds = await bookingRepo.GetPendingBookingIdsAsync(stoppingToken);
			}

			if (pendingIds.Any())
			{
				_logger.LogInformation("Найдено {Count} бронирований в статусе Pending", pendingIds.Count);

				// Для каждой брони создаём отдельный scope и задачу
				var tasks = pendingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
				await Task.WhenAll(tasks);
			}

			await Task.Delay(PollingInterval, stoppingToken);
		}
	}

	/// <summary>
	/// Обработка одной брони: задержка → чтение → подтверждение → сохранение.
	/// </summary>
	private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
	{
		_logger.LogDebug("Начало обработки брони {bookingId}", bookingId);

		// Создаём изолированный scope для обработки конкретной брони
		using var scope = _scopeFactory.CreateScope();
		var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

		try
		{
			// Имитация отложенной обработки
			await Task.Delay(ProcessingDelayMs, stoppingToken);

			var booking = await bookingRepo.GetTrackedByIdAsync(bookingId, stoppingToken);
			if (booking == null) return;

			var eventEntity = await bookingRepo.GetEventByIdAsync(booking.EventId, stoppingToken);
			if (eventEntity == null)
			{
				// Мероприятие удалено — отклоняем бронь
				booking.Reject();
				await bookingRepo.SaveChangesAsync(stoppingToken);
				_logger.LogWarning("Событие удалено. Бронь {Id} отклонена", bookingId);
				return;
			}

			// Подтверждаем бронь
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
			// Непредвиденная ошибка — логируем, не крашим сервис
			_logger.LogError(ex, "Ошибка при обработке брони {bookingId}", bookingId);
		}
	}
}