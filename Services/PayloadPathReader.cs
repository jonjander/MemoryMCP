using System.Text;
using System.Text.Json;

namespace MemoryMCP.Services;

public static class PayloadPathReader
{
    public static async Task<string> ReadTextFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Path is required.");

        var fullPath = Path.GetFullPath(path.Trim());
        if (!File.Exists(fullPath))
            throw new InvalidOperationException($"File not found: {fullPath}");

        var fileInfo = new FileInfo(fullPath);
        if (fileInfo.Length > MemoryLimits.MaxFileBytes)
        {
            throw new InvalidOperationException(
                $"File exceeds maximum size of {MemoryLimits.MaxFileBytes / (1024 * 1024)} MB ({fileInfo.Length:N0} bytes).");
        }

        await using var stream = File.OpenRead(fullPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        RawTextHelper.ValidateLength(text);
        return text;
    }

    public static async Task<T> ReadJsonFileAsync<T>(
        string path,
        JsonSerializerOptions options,
        CancellationToken cancellationToken = default)
    {
        var json = await ReadTextFileAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, options)
            ?? throw new InvalidOperationException($"Failed to deserialize JSON from '{path}'.");
    }
}
