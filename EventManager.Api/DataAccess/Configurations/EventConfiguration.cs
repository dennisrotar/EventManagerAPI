using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManagerAPI.DataAccess.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
	public void Configure(EntityTypeBuilder<Event> builder)
	{
		builder.ToTable("events");

		builder.HasKey(e => e.Id);
		// Мы генерируем Id в коде (фабрики), БД не должна подставлять свои значения
		builder.Property(e => e.Id).ValueGeneratedNever();

		builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
		builder.Property(e => e.Description).HasMaxLength(2000);

		builder.HasMany(e => e.Bookings)
			   .WithOne(b => b.Event)
			   .HasForeignKey(b => b.EventId);
	}
}