using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities; // Пространство имен сервиса Users

namespace Users.Infrastructure.DataAccess;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<User> Users => Set<User>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
	}
}