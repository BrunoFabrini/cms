using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Application.Models.Content;

public class ContentModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("content")]
    public JsonElement Content { get; set; }
}
