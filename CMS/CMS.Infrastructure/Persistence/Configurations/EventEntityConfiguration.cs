using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public class EventEntityConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("Events");
        builder.HasKey(x => x.Id).HasName("PK_Event_EventId");
        builder.Property(x => x.Id).HasColumnName("ContentId").ValueGeneratedOnAdd();

        builder.Property(x => x.Payload)
            .HasColumnType("varchar(max)")
            .IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.Property(x => x.ProcessingAttempts)
            .HasColumnType("int")
            .IsRequired();
        builder.Property(x => x.ErrorMessage)
            .HasColumnType("varchar(250)")
            .IsRequired(false);
        builder.Property(x => x.LastProcessedAtDateTime)
            .HasColumnType("datetimeoffset(2)");

        builder.HasIndex(x => new { x.Status, x.CreatedDateTime })
            .HasDatabaseName("IX_Events_Status_CreatedDateTime");

        builder.HasIndex(x => new { x.Status, x.LastProcessedAtDateTime })
            .HasDatabaseName("IX_Events_Status_LastProcessedAtDateTime");
    }
}
