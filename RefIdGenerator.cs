using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MemoryMCP;

/// <summary>
/// Short Base64url reference ids for agent-facing APIs. Internal primary keys remain Guid.
/// </summary>
public static class RefIdGenerator
{
    public const int ByteLength = 6;
    public const int CharLength = 8;

    private static readonly Regex ValidRefPattern = new("^[A-Za-z0-9_-]{8}$", RegexOptions.Compiled);

    public static string New()
    {
        Span<byte> bytes = stackalloc byte[ByteLength];
        RandomNumberGenerator.Fill(bytes);
        return ToBase64Url(bytes);
    }

    public static bool IsValidFormat(string? value) =>
        !string.IsNullOrWhiteSpace(value) && ValidRefPattern.IsMatch(value);

    private static string ToBase64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
