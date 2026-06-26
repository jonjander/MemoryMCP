namespace MemoryMCP;

public class EntityRelationship
{
    public Guid Id { get; set; }

    public Guid FromEntityId { get; set; }
    public Entity FromEntity { get; set; } = null!;

    public Guid ToEntityId { get; set; }
    public Entity ToEntity { get; set; } = null!;

    public string RelationType { get; set; } = string.Empty;

    public Guid? MemoryId { get; set; }
    public Memory? Memory { get; set; }

    public float Confidence { get; set; }
    public DateTime Created { get; set; }
}
