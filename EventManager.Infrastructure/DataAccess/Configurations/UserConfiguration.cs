using EventManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManager.Infrastructure.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.HasKey(u => u.Id);

		// Уникальный индекс на логин
		builder.HasIndex(u => u.Login).IsUnique();

		builder.Property(u => u.Login).HasMaxLength(100).IsRequired();
		builder.Property(u => u.PasswordHash).IsRequired();
		builder.Property(u => u.Role).HasConversion<string>().IsRequired();
	}
}