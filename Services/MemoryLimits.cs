namespace MemoryMCP.Services;

public static class MemoryLimits
{
    /// <summary>Maximum characters allowed in Memory.Raw and SuccessorRaw.</summary>
    public const int MaxRawChars = 2_000_000;

    /// <summary>Maximum UTF-8 file size for rawPath / bundlePath reads.</summary>
    public const int MaxFileBytes = 2 * 1024 * 1024;

    /// <summary>Characters returned in list/search summary DTOs.</summary>
    public const int PreviewChars = 500;

    /// <summary>Default slice size for get_memory when raw is large.</summary>
    public const int DefaultGetMaxChars = 32_000;
}
