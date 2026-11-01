using CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CmsDbContext))]
partial class CmsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.14")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("CMS.Domain.Entities.ContentEntity", b =>
        {
            b.Property<int?>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .HasColumnName("ContentId");

            b.Property<DateTimeOffset>("CreatedDateTime")
                .HasColumnType("datetimeoffset")
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            b.Property<bool>("Enabled")
                .HasColumnType("bit");

            b.Property<string>("PublicId")
                .IsRequired()
                .HasColumnType("varchar(100)");

            b.Property<bool>("Published")
                .HasColumnType("bit");

            b.HasKey("Id")
                .HasName("PK_Content_ContentId");

            b.ToTable("Contents");
        });

        modelBuilder.Entity("CMS.Domain.Entities.ContentVersionEntity", b =>
        {
            b.Property<int?>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .HasColumnName("ContentVersionId");

            b.Property<string>("Content")
                .IsRequired()
                .HasColumnType("varchar(max)");

            b.Property<int>("ContentId")
                .HasColumnType("int");

            b.Property<DateTimeOffset>("CreatedDateTime")
                .HasColumnType("datetimeoffset")
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            b.Property<bool>("IsPublished")
                .HasColumnType("bit");

            b.Property<DateTimeOffset>("Timestamp")
                .ValueGeneratedOnAdd()
                .HasColumnType("datetimeoffset")
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            b.Property<int>("VersionNumber")
                .HasColumnType("int");

            b.HasIndex("ContentId");

            b.HasKey("Id")
                .HasName("PK_ContentVersion_ContentVersionId");

            b.ToTable("ContentVersions");
        });

        modelBuilder.Entity("CMS.Domain.Entities.EventEntity", b =>
        {
            b.Property<int?>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .HasColumnName("ContentId");

            b.Property<DateTimeOffset>("CreatedDateTime")
                .HasColumnType("datetimeoffset")
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            b.Property<string>("ErrorMessage")
                .IsRequired(false)
                .HasColumnType("varchar(250)");

            b.Property<DateTimeOffset?>("LastProcessedAtDateTime")
                .HasColumnType("datetimeoffset(2)");

            b.Property<string>("Payload")
                .IsRequired()
                .HasColumnType("varchar(max)");

            b.Property<EventStatus>("Status")
                .HasConversion<byte>()
                .HasColumnType("tinyint");

            b.HasKey("Id")
                .HasName("PK_Event_EventId");

            b.ToTable("Events");
        });

        modelBuilder.Entity("CMS.Domain.Entities.ContentVersionEntity", b =>
        {
            b.HasOne("CMS.Domain.Entities.ContentEntity", "ContentEntity")
                .WithMany("ContentVersions")
                .HasForeignKey("ContentId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("ContentEntity");
        });

        modelBuilder.Entity("CMS.Domain.Entities.ContentEntity", b =>
        {
            b.Navigation("ContentVersions");
        });
    }
}
