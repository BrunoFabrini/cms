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
    }
}
