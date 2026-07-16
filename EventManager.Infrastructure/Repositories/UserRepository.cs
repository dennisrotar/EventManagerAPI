using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
	private readonly AppDbContext _context;

	public UserRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<User?> GetByLoginAsync(string login, CancellationToken ct)
	{
		return await _context.Users.FirstOrDefaultAsync(u => u.Login == login, ct);
	}

	public async Task AddAsync(User user, CancellationToken ct)
	{
		await _context.Users.AddAsync(user, ct);
	}

	public async Task SaveChangesAsync(CancellationToken ct)
	{
		await _context.SaveChangesAsync(ct);
	}
}