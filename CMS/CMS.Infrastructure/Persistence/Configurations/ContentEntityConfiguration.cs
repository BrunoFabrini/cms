using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public class ContentEntityConfiguration : IEntityTypeConfiguration<ContentEntity>
{
    public void Configure(EntityTypeBuilder<ContentEntity> builder)
    {
        builder.ToTable("Contents");
        builder.HasKey(x => x.Id).HasName("PK_Content_ContentId");
        builder.Property(x => x.Id).HasColumnName("ContentId").ValueGeneratedOnAdd();

        builder.Property(x => x.PublicId).HasColumnType("varchar(100)");
        builder.Property(x => x.Published).HasColumnType("bit");
        builder.Property(x => x.Enabled).HasColumnType("bit");
    }
}
