namespace CMS.Application.Models;

public class CmsEvent
{
    public string Type { get; set; } = null!;

    public string Id { get; set; } = null!;

    public int Version { get; set; }

    public DateTime Timestamp { get; set; }
}