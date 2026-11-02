using CMS.Application.Models.Events;
using CMS.Domain.Enums;
using Shouldly;
using System.Text.Json;

namespace CMS.Application.Tests;

public class CmsEventModelTests
{
    [Fact]
    public void Serialization_UsesTheExpectedEventContract()
    {
        var cmsEvent = new CmsEventModel
        {
            PublicId = "content-123",
            Type = EventType.Publish,
            Payload = new { title = "Example" },
            Version = 2,
            Timestamp = DateTimeOffset.Parse("2026-01-15T10:30:00Z")
        };

        var json = JsonSerializer.Serialize(cmsEvent);

        json.ShouldContain("\"Id\":\"content-123\"");
        json.ShouldContain("\"Type\":\"Publish\"");
        json.ShouldContain("\"Version\":2");
        json.ShouldNotContain("\"PublicId\"");
    }
}
