using Microsoft.EntityFrameworkCore;
using CMS.Domain.Entities;
using CMS.Domain.Entities.Base;
using CMS.Infrastructure.Persistence.Configurations;

namespace CMS.Infrastructure.Persistence;

public class CmsDbContext(
    DbContextOptions<CmsDbContext> options
) : DbContext(options)
{
    public DbSet<EventEntity> Events { get; set; } = null!;
    public DbSet<ContentEntity> Contents { get; set; } = null!;
    public DbSet<ContentVersionEntity> ContentVersions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContentVersionEntityConfiguration());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => typeof(EntityBase).IsAssignableFrom(entityType.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(EntityBase.CreatedDateTime))
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");
        }
    }
}