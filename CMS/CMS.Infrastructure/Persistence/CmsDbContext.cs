using Microsoft.EntityFrameworkCore;
using CMS.Domain.Entities;
using CMS.Infrastructure.Persistence.Configurations;

namespace CMS.Infrastructure.Persistence;

public class CmsDbContext(
    DbContextOptions<CmsDbContext> options
) : DbContext(options)
{
    public DbSet<ContentEntity> Contents { get; set; } = null!;
    public DbSet<ContentVersionEntity> ContentVersions { get; set; } = null!;
    public DbSet<ContentDisabledEntity> ContentsDisabled { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ContentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContentVersionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContentDisabledEntityConfiguration());
    }
}