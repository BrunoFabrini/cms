using CMS.Domain.Enums;
using System.Text.Json.Serialization;

namespace CMS.Application.Models;

public class CmsEvent
{
    [JsonPropertyName("Id")]
    public string PublicId { get; set; } = null!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventType Type { get; set; }

    public object? Payload { get; set; }

    public int? Version { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}