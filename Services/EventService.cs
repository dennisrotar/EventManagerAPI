using EventManagerAPI.Exceptions;
using EventManagerAPI.Interfaces;
using EventManagerAPI.Models;
using EventManagerAPI.Models.DTOs;

namespace EventManagerAPI.Services
{
	public class EventService : IEventService
	{
		// Данные о событиях хранятся в памяти приложения
		private readonly List<Event> _events = new();
		public List<Event> GetAll() => _events;

		//public Event? GetById(Guid id) => _events.FirstOrDefault(e => e.Id == id);
		public Event GetById(Guid id)
		{
			var eventEntity = _events.FirstOrDefault(ev => ev.Id == id) ?? throw new NotFoundException($"Мероприятие с ID {id} не найдено");
			return eventEntity;
		}

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

		public Event? Update(Guid id, UpdateEventRequestDto dto)
		{
			// Вызовет NotFoundException, если не найдено.
			var existingEvent = GetById(id);
			//if (existingEvent == null) return null;

			existingEvent.Title = dto.Title;
			existingEvent.Description = dto.Description;
			existingEvent.StartAt = dto.StartAt;
			existingEvent.EndAt = dto.EndAt;

			return existingEvent;
		}

		public bool Delete(Guid id)
		{
			var existingEvent = _events.FirstOrDefault(ev => ev.Id == id);
			//var existingEvent = GetById(id);

			if (existingEvent == null) { return false; }

			_events.Remove(existingEvent);
			return true;
		}

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
