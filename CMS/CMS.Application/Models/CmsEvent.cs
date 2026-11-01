using System.Text.Json.Serialization;

namespace CMS.Application.Models;

public class CmsEvent
{
    [JsonPropertyName("Id")]
    public string PublicId { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Payload { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}