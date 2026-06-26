namespace MemoryMCP;

public class MemoryToken
{
    public Guid Id { get; set; }

    public Guid MemoryId { get; set; }

    public Memory Memory { get; set; } = null!;

    public Guid TokenId { get; set; }

    public Token Token { get; set; } = null!;
}
