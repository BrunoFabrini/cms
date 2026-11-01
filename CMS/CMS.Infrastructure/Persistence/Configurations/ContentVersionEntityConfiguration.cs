using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public class ContentVersionEntityConfiguration : IEntityTypeConfiguration<ContentVersionEntity>
{
    public void Configure(EntityTypeBuilder<ContentVersionEntity> builder)
    {
        builder.ToTable("ContentVersions");
        builder.HasKey(x => x.Id).HasName("PK_ContentVersion_ContentVersionId");
        builder.Property(x => x.Id).HasColumnName("ContentVersionId").ValueGeneratedOnAdd();

        builder.Property(x => x.ContentId).HasColumnType("int");
        builder.Property(x => x.VersionNumber).HasColumnType("int");
        builder.Property(x => x.Content).HasColumnType("varchar(max)");
        builder.Property(x => x.Timestamp)
            .HasColumnType("datetimeoffset")
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(x => x.Published).HasColumnType("bit");

        builder.HasOne(x => x.ContentEntity)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ContentId);
    }
}
