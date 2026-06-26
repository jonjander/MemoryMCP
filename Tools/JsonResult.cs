using System.Text.Json;
using System.Text.Json.Serialization;

namespace MemoryMCP.Tools;

internal static class JsonResult
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Ok(object value) => JsonSerializer.Serialize(value, Options);

    public static string OkWithNextSteps(object value, IReadOnlyList<string> nextSteps) =>
        JsonSerializer.Serialize(new { result = value, nextSteps }, Options);
}
