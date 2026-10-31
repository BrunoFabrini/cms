using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public class ContentDisabledEntityConfiguration : IEntityTypeConfiguration<ContentDisabledEntity>
{
    public void Configure(EntityTypeBuilder<ContentDisabledEntity> builder)
    {
        builder.ToTable("ContentsDisabled");
        builder.HasKey(x => x.Id).HasName("PK_ContentDisabled_ContentDisabledId");
        builder.Property(x => x.Id).HasColumnName("ContentDisabledId").ValueGeneratedOnAdd();
    }
}
