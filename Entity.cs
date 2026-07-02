namespace MemoryMCP;

public class Entity : IHasRef
{
    public Guid Id { get; set; }

    /// <summary>Short Base64url alias for agent-facing APIs (8 chars). Guid remains internal PK.</summary>
    public string? Ref { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime? Updated { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public Guid? MergedIntoEntityId { get; set; }

    public Entity? MergedIntoEntity { get; set; }

    public ICollection<MemoryEntity> Memories { get; set; } = [];

    public ICollection<EntityRelationship> OutgoingRelationships { get; set; } = [];

    public ICollection<EntityRelationship> IncomingRelationships { get; set; } = [];

    public ICollection<EntityRevision> Revisions { get; set; } = [];
}

public enum EntityStatus
{
    Active,
    Merged,
    Deprecated
}
