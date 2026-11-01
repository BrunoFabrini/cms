using CMS.Domain.Entities.Base;

namespace CMS.Domain.Entities;

public class ContentVersionEntity : EntityBase
{
    public int ContentId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; }
    public bool IsPublished { get; set; }
    public ContentEntity ContentEntity { get; set; } = null!;
}
