namespace MemoryMCP;

public sealed record ServerStartupOptions
{
    public const string DefaultDbFileName = "memory.db";

    public string DbFileName { get; init; } = DefaultDbFileName;

    /// <summary>Full name (för- och efternamn) for the MCP user — resolves "jag" / "I" in agent guidance.</summary>
    public string? WhoAmI { get; init; }

    /// <summary>
    /// When set, new memories are written with this key and reads are scoped to it only.
    /// When null, writes use null partition and reads see all partitions (including null).
    /// </summary>
    public string? Partition { get; init; }
}
