using Events.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Events.Infrastructure.Cache;

/// <summary>
/// Реализация ICacheService через Redis.
/// Обрабатывает ошибки Redis, чтобы сервис деградировал в БД без падения.
/// </summary>
public class RedisCacheService : ICacheService
{
	private readonly IConnectionMultiplexer _redis;
	private readonly ILogger<RedisCacheService> _logger;

	public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
	{
		_redis = redis;
		_logger = logger;
	}

	public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
	{
		try
		{
			var db = _redis.GetDatabase();
			var value = await db.StringGetAsync(key);

			if (value.IsNullOrEmpty)
				return default;

			return JsonSerializer.Deserialize<T>(value.ToString()!);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при получении данных из Redis по ключу {Key}.", key);
			return default; // Возвращаем null, запрос пойдёт в БД
		}
	}

	public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
	{
		try
		{
			var db = _redis.GetDatabase();
			var json = JsonSerializer.Serialize(value);
			await db.StringSetAsync(key, json, ttl);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при сохранении данных в Redis по ключу {Key}.", key);
			// Не пробрасываем исключение
		}
	}

	public async Task RemoveAsync(string key, CancellationToken ct)
	{
		try
		{
			var db = _redis.GetDatabase();
			await db.KeyDeleteAsync(key);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при удалении ключа {Key} из Redis.", key);
		}
	}
}