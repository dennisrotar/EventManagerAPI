using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagerAPI.DataAccess.Configurations;

internal sealed class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<Event> Events => Set<Event>();
	public DbSet<Booking> Bookings => Set<Booking>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Автоматически подхватит все классы IEntityTypeConfiguration из этой сборки
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
	}
}