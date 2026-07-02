namespace MemoryMCP;

public class Memory : IHasRef
{
    public Guid Id { get; set; }

    /// <summary>Short Base64url alias for agent-facing APIs (8 chars). Guid remains internal PK.</summary>
    public string? Ref { get; set; }

    public string Raw { get; set; } = string.Empty;

    public List<MemoryToken> Tokens { get; set; } = [];

    public ICollection<MemoryEntity> Entities { get; set; } = [];

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public DateTime? MemoryFrom { get; set; }

    public MemoryStatus Status { get; set; } = MemoryStatus.Active;

    public string? StatusNote { get; set; }

    public Guid? SupersedesMemoryId { get; set; }

    public Memory? SupersedesMemory { get; set; }

    public Guid? SupersededByMemoryId { get; set; }

    public Memory? SupersededByMemory { get; set; }

    public ICollection<MemoryRevision> Revisions { get; set; } = [];
}

public enum MemoryStatus
{
    Active,
    Superseded,
    Invalid,
    Retracted
}
