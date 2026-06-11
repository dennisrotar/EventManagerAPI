using EventManagerAPI.Models.Entities;

namespace EventManagerAPI.Repositories;

public interface IEventRepository
{
    Task<(List<Event> Items, int TotalCount)> GetFilteredAsync(
        string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
    
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(Event eventEntity);
    void Update(Event eventEntity);
    void Remove(Event eventEntity);
    Task SaveChangesAsync(CancellationToken ct);
}