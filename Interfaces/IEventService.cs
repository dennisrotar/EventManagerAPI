using EventManagerAPI.Models;
using EventManagerAPI.Models.DTOs;

namespace EventManagerAPI.Interfaces
{
	public interface IEventService
	{
		List<Event> GetAll();
		Event? GetById(Guid id);
		Event Create(CreateEventRequestDto dto);
		Event? Update(Guid id, UpdateEventRequestDto dto);
		bool Delete(Guid id);

		// Новый метод для фильтрации и пагинации
		PaginatedResultDto<Event> GetFiltered(GetEventsQueryParams query);
	}
}
