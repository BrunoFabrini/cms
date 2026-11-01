using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Infrastructure.Utils;

public static class JsonConfigs
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
