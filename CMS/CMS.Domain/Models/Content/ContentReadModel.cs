namespace CMS.Domain.Models.Content;

public class ContentReadModel
{
    public string PublicId { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Content { get; set; } = string.Empty;
}
