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
	}
}
