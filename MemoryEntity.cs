namespace MemoryMCP;

public class MemoryEntity
{
    public Guid MemoryId { get; set; }

    public Memory Memory { get; set; } = null!;

    public Guid EntityId { get; set; }

    public Entity Entity { get; set; } = null!;
}
