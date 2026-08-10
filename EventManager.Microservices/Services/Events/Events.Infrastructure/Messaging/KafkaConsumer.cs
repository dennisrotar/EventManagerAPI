using Confluent.Kafka;
using EventManager.Shared.Events;
using EventManager.Shared.Topics;
using Events.Application.Interfaces;
using Events.Infrastructure.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Events.Infrastructure.Messaging
{
	public class KafkaConsumer : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IConfiguration _config;
		private readonly ILogger<KafkaConsumer> _logger;
		private IConsumer<string, string>? _consumer;

		public KafkaConsumer(IServiceProvider serviceProvider, IConfiguration config, ILogger<KafkaConsumer> logger)
		{
			_serviceProvider = serviceProvider;
			_config = config;
			_logger = logger;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var config = new ConsumerConfig
			{
				BootstrapServers = _config["Kafka:BootstrapServers"],
				GroupId = _config["Kafka:ConsumerGroup"],
				AutoOffsetReset = AutoOffsetReset.Earliest
			};

			_consumer = new ConsumerBuilder<string, string>(config).Build();
			_consumer.Subscribe(KafkaTopics.BookingConfirmed);

			return Task.Run(async () =>
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					try
					{
						var consumeResult = _consumer.Consume(stoppingToken);
						var @event = JsonSerializer.Deserialize<BookingConfirmedEvent>(consumeResult.Message.Value);

						if (@event == null) continue;

						using var scope = _serviceProvider.CreateScope();
						var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
						var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

						// 1. Проверяем идемпотентность
						var existingRecord = await dbContext.ProcessedMessages.FindAsync(@event.BookingId);
						if (existingRecord != null)
						{
							_logger.LogInformation("Сообщение {BookingId} уже обработано. Пропуск.", @event.BookingId);
							_consumer.Commit(consumeResult);
							continue;
						}

						// 2. Уменьшаем места
						var eventEntity = await dbContext.Events.FindAsync(@event.EventId);
						if (eventEntity == null)
						{
							_logger.LogWarning("Событие {EventId} не найдено. Пропуск.", @event.EventId);
							_consumer.Commit(consumeResult);
							continue;
						}

						if (!eventEntity.TryReserveSeats(@event.SeatsBooked))
						{
							_logger.LogError("Недостаточно мест для EventId {EventId}. Пропуск.", @event.EventId);
							_consumer.Commit(consumeResult);
							continue;
						}

						// 3. Сохраняем ID обработанного сообщения и изменения в одной транзакции
						dbContext.ProcessedMessages.Add(new Events.Domain.Entities.ProcessedMessage
						{
							Id = @event.BookingId,
							MessageType = nameof(BookingConfirmedEvent),
							ProcessedAt = DateTime.UtcNow
						});

						// 1. Сохраняем в БД
						await dbContext.SaveChangesAsync(stoppingToken);

						// 2. Инвалидируем кеш конкретного события и топ-10
						await cacheService.RemoveAsync($"event:{@event.EventId}", stoppingToken);
						await cacheService.RemoveAsync("events:top10", stoppingToken);

						_consumer.Commit(consumeResult);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Ошибка при обработке сообщения Kafka");
					}
				}
			}, stoppingToken);
		}

		public override void Dispose()
		{
			_consumer?.Close();
			_consumer?.Dispose();
			base.Dispose();
		}
	}
}