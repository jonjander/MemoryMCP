namespace MemoryMCP;

public sealed record ServerStartupOptions
{
    public const string DefaultDbFileName = "memory.db";

    public string DbFileName { get; init; } = DefaultDbFileName;

    /// <summary>Full name (för- och efternamn) for the MCP user — resolves "jag" / "I" in agent guidance.</summary>
    public string? WhoAmI { get; init; }
}
