using EventManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManager.Infrastructure.DataAccess.Configurations;

/// <summary>
/// EF Core конфигурация маппинга сущности Booking на таблицу bookings.
/// Находится в Infrastructure, так как зависит от EF Core (внешняя технология).
/// </summary>
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
	public void Configure(EntityTypeBuilder<Booking> builder)
	{
		builder.ToTable("bookings");

		builder.HasKey(b => b.Id);
		// Id генерируется в коде (фабричный метод), БД не должна подставлять свои значения
		builder.Property(b => b.Id).ValueGeneratedNever();

		// Сохраняем Enum в БД как строку ("Pending", "Confirmed", "Cancelled")
		builder.Property(b => b.Status)
			   .HasConversion<string>()
			   .HasMaxLength(50)
			   .IsRequired();

		// Связь: Booking → Event (многие к одному)
		builder.HasOne(b => b.Event)
				.WithMany(e => e.Bookings)
				.HasForeignKey(b => b.EventId);

		builder.HasOne<User>()
				.WithMany()
				.HasForeignKey(b => b.UserId)
				.OnDelete(DeleteBehavior.Cascade);
	}
}