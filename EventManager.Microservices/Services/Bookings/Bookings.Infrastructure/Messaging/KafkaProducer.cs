using Bookings.Application.Interfaces;
using Confluent.Kafka;
using EventManager.Shared.Events;
using EventManager.Shared.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bookings.Infrastructure.Messaging
{
	public class KafkaProducer : IEventPublisher, IDisposable
	{
		private readonly IProducer<string, string> _producer;
		private readonly ILogger<KafkaProducer> _logger;

		public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
		{
			var config = new ProducerConfig
			{
				BootstrapServers = configuration["Kafka:BootstrapServers"]
			};
			_producer = new ProducerBuilder<string, string>(config).Build();
			_logger = logger;
		}

		public async Task PublishBookingConfirmedAsync(BookingConfirmedEvent @event, Guid eventId)
		{
			var message = new Message<string, string>
			{
				Key = eventId.ToString(), // Ключ = EventId для порядка в партиции
				Value = JsonSerializer.Serialize(@event)
			};

			var result = await _producer.ProduceAsync(KafkaTopics.BookingConfirmed, message);
			_logger.LogInformation("Опубликовано событие BookingConfirmed для EventId: {EventId}, Partition: {Partition}, Offset: {Offset}",
				eventId, result.Partition, result.Offset);
		}

		public void Dispose()
		{
			_producer?.Flush();
			_producer?.Dispose();
		}
	}
}