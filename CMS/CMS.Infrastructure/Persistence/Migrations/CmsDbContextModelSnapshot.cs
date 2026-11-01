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
                .HasColumnType("datetimeoffset");

            b.HasKey("Id")
                .HasName("PK_Content_ContentId");

            b.ToTable("Contents");
        });

        modelBuilder.Entity("CMS.Domain.Entities.ContentDisabledEntity", b =>
        {
            b.Property<int?>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .HasColumnName("ContentDisabledId");

            b.Property<DateTimeOffset>("CreatedDateTime")
                .HasColumnType("datetimeoffset");

            b.HasKey("Id")
                .HasName("PK_ContentDisabled_ContentDisabledId");

            b.ToTable("ContentsDisabled");
        });

        modelBuilder.Entity("CMS.Domain.Entities.ContentVersionEntity", b =>
        {
            b.Property<int?>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .HasColumnName("ContentVersionId");

            b.Property<DateTimeOffset>("CreatedDateTime")
                .HasColumnType("datetimeoffset");

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
                .HasColumnType("datetimeoffset");

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
    }
}
