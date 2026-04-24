using EventManagerAPI.DataAccess;

namespace EventManagerAPI.Services;

/// <summary>
/// Фоновый сервис для имитации отложенной обработки бронирований.
/// Опрашивает хранилище и переводит Pending брони в Confirmed.
/// </summary>
public class BookingBackgroundService : BackgroundService
{
	private readonly IBookingStore _bookingStore;
	private readonly ILogger<BookingBackgroundService> _logger;

	public BookingBackgroundService(IBookingStore bookingStore, ILogger<BookingBackgroundService> logger)
	{
		_bookingStore = bookingStore;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Фоновый сервис обработки бронирований запущен.");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var pendingBookings = _bookingStore.GetPendingBookings().ToList();

				foreach (var booking in pendingBookings)
				{
					if (stoppingToken.IsCancellationRequested) break;

					_logger.LogInformation("Обработка брони {BookingId}... Имитация вызова внешней системы.", booking.Id);

					// Искусственная задержка 2 секунды (имитация внешнего API)
					// ВАЖНО: передаем stoppingToken, чтобы сервис мгновенно завершался при остановке приложения
					await Task.Delay(2000, stoppingToken);

					// Меняем статус через доменный метод
					booking.Confirm();
					_bookingStore.Update(booking);

					_logger.LogInformation("Бронь {BookingId} успешно подтверждена.", booking.Id);
				}
			}
			catch (OperationCanceledException)
			{
				// Нормальное поведение при остановке приложения (Ctrl+C)
				_logger.LogInformation("Фоновый сервис обработки бронирований останавливается.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при обработке бронирований в фоновом сервисе.");
			}

			// Пауза перед следующей проверкой (5 секунд)
			if (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(5000, stoppingToken);
			}
		}
	}
}