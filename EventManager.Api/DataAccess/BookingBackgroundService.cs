using EventManagerAPI.Models.Entities;

namespace EventManagerAPI.DataAccess;

/// <summary>
/// Фоновый сервис для имитации отложенной обработки бронирований.
/// Опрашивает хранилище и переводит Pending брони в Confirmed.
/// </summary>
public class BookingBackgroundService : BackgroundService
{
	private readonly IBookingStore _bookingStore;
	private readonly IEventStore _eventStore;
	private readonly ILogger<BookingBackgroundService> _logger;

	// Асинхронный примитив для защиты записи в хранилище во время параллельной обработки
	private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

	public BookingBackgroundService(IBookingStore bookingStore, IEventStore eventStore, ILogger<BookingBackgroundService> logger)
	{
		_bookingStore = bookingStore;
		_eventStore = eventStore;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Фоновый сервис обработки бронирований запущен.");

		while (!stoppingToken.IsCancellationRequested)
		{
			var pendingBookings = _bookingStore.GetPendingBookings().ToList();

			if (pendingBookings.Any())
			{
				_logger.LogInformation("Найдено {Count} бронирований в статусе Pending", pendingBookings.Count);

				// Запускаем обработку ПАРАЛЛЕЛЬНО
				var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
				await Task.WhenAll(tasks);
			}

			await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
		}
	}

	private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
	{
		_logger.LogDebug("Начало обработки брони {BookingId}", booking.Id);

		try
		{
			// Имитация вызова внешней системы ВНЕ семафора (чтобы задержки были параллельными)
			await Task.Delay(2000, stoppingToken);

			// Захватываем семафор только перед изменением состояния/хранилища
			await _processingSemaphore.WaitAsync(stoppingToken);
			try
			{
				// Проверяем, существует ли еще событие
				var eventEntity = _eventStore.GetById(booking.EventId);
				if (eventEntity == null)
				{
					_logger.LogWarning("Событие {EventId} удалено. Отклонение брони {BookingId}", booking.EventId, booking.Id);
					booking.Reject();
					_bookingStore.Update(booking);
					return;
				}

				// Подтверждаем бронь
				booking.Confirm();
				_bookingStore.Update(booking);
				_logger.LogInformation("Бронь {BookingId} подтверждена", booking.Id);
			}
			finally
			{
				_processingSemaphore.Release();
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Обработка брони {BookingId} отменена", booking.Id);
		}
		catch (Exception ex)
		{
			// Откат при непредвиденной ошибке: отклоняем бронь и возвращаем место
			_logger.LogError(ex, "Ошибка при обработке брони {BookingId}", booking.Id);

			await _processingSemaphore.WaitAsync(stoppingToken);
			try
			{
				var eventEntity = _eventStore.GetById(booking.EventId);
				if (eventEntity != null) eventEntity.ReleaseSeats(); // Возвращаем место

				booking.Reject();
				_bookingStore.Update(booking);
			}
			finally
			{
				_processingSemaphore.Release();
			}
		}
	}

}