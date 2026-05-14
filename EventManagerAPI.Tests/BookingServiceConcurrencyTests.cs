using EventManagerAPI.DataAccess;
using EventManagerAPI.Exceptions;
using EventManagerAPI.Models.DTOs;
using EventManagerAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManagerAPI.Tests;

/// <summary>
/// Класс юнит-тестов для проверки конкурентности в BookingService.
/// Использует реальные инстансы InMemory хранилищ для проверки работы примитивов синхронизации (lock).
/// </summary>
public class BookingServiceConcurrencyTests
{
	private readonly InMemoryEventStore _eventStore;
	private readonly InMemoryBookingStore _bookingStore;
	private readonly EventService _eventService;
	private readonly BookingService _bookingService;

	public BookingServiceConcurrencyTests()
	{
		// Создаем общие хранилища
		_eventStore = new InMemoryEventStore();
		_bookingStore = new InMemoryBookingStore();

		// Создаем сервисы, передавая им ОДНО И ТО ЖЕ хранилище событий.
		_eventService = new EventService(_eventStore, NullLogger<EventService>.Instance);
		_bookingService = new BookingService(_bookingStore, _eventStore, NullLogger<BookingService>.Instance);
	}

	/// <summary>
	/// Вспомогательный метод для создания тестового события с заданным количеством мест.
	/// </summary>
	/// <param name="totalSeats">Общее количество мест для создания.</param>
	/// <returns>ID созданного события.</returns>
	private Guid CreateTestEventWithSeats(int totalSeats)
	{
		var dto = new CreateEventRequestDto
		{
			Title = "Concert",
			StartAt = DateTime.UtcNow.AddDays(1),
			EndAt = DateTime.UtcNow.AddDays(2),
			TotalSeats = totalSeats
		};

		var response = _eventService.Create(dto);
		return response.Id;
	}

	/// <summary>
	/// Проверяет, что успешное создание брони синхронно уменьшает количество доступных мест в хранилище ровно на 1.
	/// </summary>
	[Fact]
	public async Task CreateBooking_ShouldDecrementAvailableSeats()
	{
		// Arrange
		var eventId = CreateTestEventWithSeats(5);
		var eventBefore = _eventStore.GetById(eventId)!;

		// Доп. проверка, чтобы тест был независимым
		Assert.Equal(5, eventBefore.AvailableSeats);

		// Act
		await _bookingService.CreateBookingAsync(eventId);
		var eventAfter = _eventStore.GetById(eventId)!;

		// Assert
		Assert.Equal(4, eventAfter.AvailableSeats); // Ожидаем строго 4
	}

	/// <summary>
	/// Проверяет, что попытка забронировать место при нулевом количестве свободных мест 
	/// выбрасывает NoAvailableSeatsException (ошибка 409 Conflict).
	/// </summary>
	[Fact]
	public async Task CreateBooking_WhenNoSeats_ShouldThrowNoAvailableSeatsException()
	{
		// Arrange
		var eventId = CreateTestEventWithSeats(1);
		await _bookingService.CreateBookingAsync(eventId); // Забираем единственное место

		// Act & Assert
		await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(eventId));
	}

	/// <summary>
	/// Проверяет защиту от овербукинга при многопоточности (Task.Run + Task.WhenAll).
	/// Запускает 20 конкурентных запросов на 5 мест.
	/// Ожидается: ровно 5 успешных броней, 15 исключений NoAvailableSeatsException, 
	/// и AvailableSeats в хранилище должно стать равно 0.
	/// </summary>
	[Fact]
	public async Task ConcurrentBookings_ShouldPreventOverbooking()
	{
		// Arrange
		var eventId = CreateTestEventWithSeats(5);
		const int concurrentTasks = 20;

		var tasks = new Task[concurrentTasks];
		var exceptions = new List<Exception>();

		// Act: Запускаем 20 потоков одновременно
		for (int i = 0; i < concurrentTasks; i++)
		{
			tasks[i] = Task.Run(async () =>
			{
				try
				{
					await _bookingService.CreateBookingAsync(eventId);
				}
				catch (Exception ex)
				{
					lock (exceptions) { exceptions.Add(ex); }
				}
			});
		}

		await Task.WhenAll(tasks);

		// Assert
		var noSeatsExceptions = exceptions.OfType<NoAvailableSeatsException>().ToList();

		// Должно быть ровно 15 ошибок (20 попыток минус 5 успешных)
		Assert.Equal(15, noSeatsExceptions.Count);

		// Все остальные исключения (если есть) должны быть только этого типа
		Assert.Equal(exceptions.Count, noSeatsExceptions.Count);

		// Проверяем состояние хранилища: свободных мест не должно остаться
		var eventEntity = _eventStore.GetById(eventId)!;
		Assert.Equal(0, eventEntity.AvailableSeats);
	}

	/// <summary>
	/// Проверяет, что при конкурентном создании броней генерируются уникальные идентификаторы (Id).
	/// Запускает 10 конкурентных запросов на 10 мест и проверяет уникальность полученных Id.
	/// </summary>
	[Fact]
	public async Task ConcurrentBookings_ShouldHaveUniqueIds()
	{
		// Arrange
		var eventId = CreateTestEventWithSeats(10);
		var tasks = Enumerable.Range(0, 10).Select(_ => _bookingService.CreateBookingAsync(eventId));

		// Act
		var results = await Task.WhenAll(tasks);

		// Assert
		var uniqueIds = results.Select(b => b.Id).ToHashSet();

		// Если бы Id генерировались не потокобезопасно, здесь было бы меньше 10 элементов
		Assert.Equal(10, uniqueIds.Count);
	}
}