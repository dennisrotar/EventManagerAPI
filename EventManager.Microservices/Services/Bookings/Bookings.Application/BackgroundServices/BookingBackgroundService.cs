using Bookings.Application.Interfaces;
using EventManager.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.BackgroundServices;

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

		// Получаем издателя kafka
		var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

		try
		{
			// Имитация отложенной обработки
			await Task.Delay(ProcessingDelayMs, stoppingToken);

			var booking = await bookingRepo.GetTrackedByIdAsync(bookingId, stoppingToken);
			if (booking == null) return;

			// Подтверждаем бронь
			booking.Confirm();
			await bookingRepo.SaveChangesAsync(stoppingToken);

			_logger.LogInformation("Бронь {Id} подтверждена в БД. Публикация в Kafka...", bookingId);

			// 2. Публикуем событие в Kafka
			var eventMessage = new BookingConfirmedEvent(
				booking.Id,
				booking.EventId,
				booking.UserId,
				1, // Количество мест. Если у тебя в Booking есть поле SeatsBooked, используй booking.SeatsBooked
				DateTime.UtcNow
			);

			await eventPublisher.PublishBookingConfirmedAsync(eventMessage, booking.EventId);
			_logger.LogInformation("Событие BookingConfirmed для брони {Id} опубликовано в Kafka.", bookingId);
		}
		catch (OperationCanceledException) { /* Игнорируем */ }
		catch (Exception ex)
		{
			// Непредвиденная ошибка — логируем, не крашим сервис
			_logger.LogError(ex, "Ошибка при обработке брони {bookingId}", bookingId);
		}
	}
}