using CMS.Domain.Entities.Base;

namespace CMS.Domain.Entities;

public class ContentEntity : EntityBase
{
    public string PublicId { get; set; } = null!;
    public bool Published { get; set; }
    public bool Enabled { get; set; }
    public ICollection<ContentVersionEntity> ContentVersions { get; set; } = new List<ContentVersionEntity>();
}
