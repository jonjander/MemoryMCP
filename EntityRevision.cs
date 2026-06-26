namespace MemoryMCP;

public class EntityRevision
{
    public Guid Id { get; set; }

    public Guid EntityId { get; set; }

    public Entity Entity { get; set; } = null!;

    public EntityRevisionType RevisionType { get; set; }

    public DateTime Created { get; set; }

    public string? Note { get; set; }

    public string? PreviousName { get; set; }

    public string? NewName { get; set; }

    public string? PreviousType { get; set; }

    public string? NewType { get; set; }

    public EntityStatus? PreviousStatus { get; set; }

    public EntityStatus? NewStatus { get; set; }

    public Guid? RelatedEntityId { get; set; }
}

public enum EntityRevisionType
{
    NameUpdated,
    TypeUpdated,
    Merged,
    Deprecated,
    Reactivated
}
