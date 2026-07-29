using Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Events.Infrastructure.DataAccess.Configurations
{
	public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
	{
		public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
		{
			builder.HasKey(pm => pm.Id);
			builder.Property(pm => pm.MessageType).IsRequired().HasMaxLength(100);
		}
	}
}