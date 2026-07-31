using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventManager.Shared.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Events.Infrastructure.Messaging
{
	public class KafkaTopicCreator : IHostedService
	{
		private readonly IConfiguration _config;
		private readonly ILogger<KafkaTopicCreator> _logger;

		public KafkaTopicCreator(IConfiguration config, ILogger<KafkaTopicCreator> logger)
		{
			_config = config;
			_logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _config["Kafka:BootstrapServers"] }).Build();
			try
			{
				await adminClient.CreateTopicsAsync(new TopicSpecification[]
				{
					new TopicSpecification { Name = KafkaTopics.BookingConfirmed, ReplicationFactor = 1, NumPartitions = 1 }
				});
				_logger.LogInformation("Топик {Topic} создан или уже существует.", KafkaTopics.BookingConfirmed);
			}
			catch (CreateTopicsException ex)
			{
				_logger.LogWarning(ex, "Не удалось создать топик (возможно, он уже существует).");
			}
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}