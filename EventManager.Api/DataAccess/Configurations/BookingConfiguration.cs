using EventManagerAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManagerAPI.DataAccess.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
	public void Configure(EntityTypeBuilder<Booking> builder)
	{
		builder.ToTable("bookings");

		builder.HasKey(b => b.Id);
		builder.Property(b => b.Id).ValueGeneratedNever();

		// Сохраняем Enum в БД как строку
		builder.Property(b => b.Status)
			   .HasConversion<string>()
			   .HasMaxLength(50)
			   .IsRequired();

		builder.HasOne(b => b.Event)
			   .WithMany(e => e.Bookings)
			   .HasForeignKey(b => b.EventId);
	}
}