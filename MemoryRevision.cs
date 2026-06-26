namespace MemoryMCP;

public class MemoryRevision
{
    public Guid Id { get; set; }

    public Guid MemoryId { get; set; }

    public Memory Memory { get; set; } = null!;

    public MemoryRevisionType RevisionType { get; set; }

    public DateTime Created { get; set; }

    public string? Note { get; set; }

    public DateTime? PreviousMemoryFrom { get; set; }

    public DateTime? NewMemoryFrom { get; set; }

    public MemoryStatus? PreviousStatus { get; set; }

    public MemoryStatus? NewStatus { get; set; }

    public Guid? SuccessorMemoryId { get; set; }

    public string? SuccessorRaw { get; set; }
}

public enum MemoryRevisionType
{
    MemoryFromUpdated,
    Invalidated,
    Retracted,
    Superseded,
    Corrected
}
