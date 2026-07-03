using EventManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManager.Infrastructure.DataAccess.Configurations;

/// <summary>
/// EF Core конфигурация маппинга сущности Event на таблицу events.
/// Находится в Infrastructure, так как зависит от EF Core (внешняя технология).
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
	public void Configure(EntityTypeBuilder<Event> builder)
	{
		builder.ToTable("events");

		builder.HasKey(e => e.Id);
		// Id генерируется в коде (фабричный метод), БД не должна подставлять свои значения
		builder.Property(e => e.Id).ValueGeneratedNever();

		builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
		builder.Property(e => e.Description).HasMaxLength(2000);

		// Связь: Event → Bookings (один ко многим)
		builder.HasMany(e => e.Bookings)
			   .WithOne(b => b.Event)
			   .HasForeignKey(b => b.EventId);
	}
}