using EventManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Infrastructure.DataAccess;

/// <summary>
/// Контекст базы данных EF Core.
/// Ссылается на доменные сущности из Domain-слоя.
/// Находится в Infrastructure, так как зависит от EF Core (внешняя технология).
/// </summary>
public class AppDbContext : DbContext
{
	/// <summary>
	/// Конструктор с опциями для конфигурации подключения.
	/// </summary>
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	/// <summary>DbSet мероприятий.</summary>
	public DbSet<Event> Events => Set<Event>();

	/// <summary>DbSet бронирований.</summary>
	public DbSet<Booking> Bookings => Set<Booking>();

	/// <summary>DbSet пользователей.</summary>
	public DbSet<User> Users => Set<User>();

	/// <summary>
	/// Конфигурация модели: автоматически подхватывает все IEntityTypeConfiguration
	/// из текущей сборки (Infrastructure).
	/// </summary>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
	}
}