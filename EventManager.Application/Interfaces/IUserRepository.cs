using EventManager.Domain.Entities;
namespace EventManager.Application.Interfaces;

public interface IUserRepository
{
	Task<User?> GetByLoginAsync(string login, CancellationToken ct);
	Task AddAsync(User user, CancellationToken ct);
	Task SaveChangesAsync(CancellationToken ct);
}