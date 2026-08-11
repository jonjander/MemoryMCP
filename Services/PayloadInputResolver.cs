namespace MemoryMCP.Services;

public static class PayloadInputResolver
{
    public static async Task<string> ResolveRawAsync(
        string? raw,
        string? rawPath,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(rawPath))
            return await PayloadPathReader.ReadTextFileAsync(rawPath, cancellationToken);

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Either raw or rawPath is required.");

        RawTextHelper.ValidateLength(raw);
        return raw;
    }
}
