using System.Text.Json;
using System.Text.Json.Serialization;

namespace MemoryMCP.Tools;

internal static class McpJson
{
    internal static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static IReadOnlyList<T> DeserializeList<T>(string json, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"{parameterName} is required.");

        return JsonSerializer.Deserialize<List<T>>(json, Options)
            ?? throw new InvalidOperationException($"{parameterName} must be a JSON array.");
    }
}
