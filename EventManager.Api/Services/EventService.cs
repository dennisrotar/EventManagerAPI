using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models;
using EventManagerAPI.Models.DTOs;

namespace EventManagerAPI.Services
{
	/// <summary>
	/// Реализация интерфейса IEventService.
	/// Хранит данные в ОЗУ (List).
	/// Зарегистрирован в DI как Singleton.
	/// </summary>
	public class EventService : IEventService
	{
		/// <summary>
		/// Список мероприятий, хранящихся в ОЗУ.
		/// </summary>
		private readonly List<Event> _events = new();

		/// <summary>
		/// Список всех имеющихся мероприятий в ОЗУ.
		/// </summary>
		/// <returns></returns>
		public List<Event> GetAll() => _events;

		/// <summary>
		/// Получить мероприятие по ID. Выбрасывает исключение, если не найдно.
		/// </summary>
		public Event GetById(Guid id)
		{
			var eventEntity = _events.FirstOrDefault(ev => ev.Id == id) ?? throw new NotFoundException($"Мероприятие с ID {id} не найдено");
			return eventEntity;
		}

		/// <summary>
		/// Добавить мероприятие в хранилище, генерирует новый Guid.
		/// </summary>
		public Event Create(CreateEventRequestDto dto)
		{
			var newEvent = new Event
			{
				Id = Guid.NewGuid(),
				Title = dto.Title,
				Description = dto.Description,
				StartAt = dto.StartAt,
				EndAt = dto.EndAt
			};

			_events.Add(newEvent);
			return newEvent;
		}

		/// <summary>
		/// Полностью обновить данные мероприятия. Выбрасывает исключение, если не найдено.
		/// </summary>
		public Event? Update(Guid id, UpdateEventRequestDto dto)
		{
			// Вызовет NotFoundException, если не найдено.
			var existingEvent = GetById(id);

			existingEvent.Title = dto.Title;
			existingEvent.Description = dto.Description;
			existingEvent.StartAt = dto.StartAt;
			existingEvent.EndAt = dto.EndAt;

			return existingEvent;
		}

		/// <summary>
		/// Удалить мероприятие из хранилища.
		/// </summary>
		public bool Delete(Guid id)
		{
			var existingEvent = _events.FirstOrDefault(ev => ev.Id == id);

			if (existingEvent == null) { return false; }

			_events.Remove(existingEvent);
			return true;
		}

		/// <summary>
		/// Сформироват и получить выборку мероприятий с использованием LINQ (Where, OrderBy, Skip, Take).
		/// </summary>
		public PaginatedResultDto<Event> GetFiltered(GetEventsQueryParams query)
		{
			var queryable = _events.AsQueryable();

			// Фильтрация:  "Логическое И"
			if (!string.IsNullOrWhiteSpace(query.Title))
			{
				queryable = queryable.Where(e => e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
			}

			if (query.From.HasValue)
			{
				queryable = queryable.Where(e => e.StartAt >= query.From.Value);
			}

			if (query.To.HasValue)
			{
				queryable = queryable.Where(e => e.EndAt <= query.To.Value);
			}

			var totalCount = queryable.Count();

			var items = queryable
				.OrderBy(e => e.StartAt)
				.Skip((query.Page - 1) * query.PageSize)
				.Take(query.PageSize)
				.ToList();

			return new PaginatedResultDto<Event>
			{
				TotalCount = totalCount,
				Page = query.Page,
				PageSize = query.PageSize,
				Items = items
			};
		}
	}
}
