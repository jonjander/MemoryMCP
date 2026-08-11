namespace MemoryMCP.Services;

public static class RawTextHelper
{
    public static void ValidateLength(string raw)
    {
        if (raw.Length > MemoryLimits.MaxRawChars)
        {
            throw new InvalidOperationException(
                $"Raw text exceeds maximum of {MemoryLimits.MaxRawChars:N0} characters ({raw.Length:N0} given).");
        }
    }

    public static (string Text, int Length, bool Truncated) ToPreview(string raw, int maxChars = MemoryLimits.PreviewChars)
    {
        var length = raw.Length;
        if (length <= maxChars)
            return (raw, length, false);

        return (raw[..maxChars] + "…", length, true);
    }

    public static (string Slice, int Length, bool Truncated) Slice(string raw, int offset, int maxChars)
    {
        if (string.IsNullOrEmpty(raw))
            return (string.Empty, 0, false);

        offset = Math.Max(0, offset);
        if (offset >= raw.Length)
            return (string.Empty, raw.Length, true);

        maxChars = Math.Max(1, maxChars);
        var take = Math.Min(maxChars, raw.Length - offset);
        var slice = raw.Substring(offset, take);
        var truncated = offset + take < raw.Length;
        return (slice, raw.Length, truncated);
    }
}
