namespace MemoryMCP;

public interface IHasRef
{
    Guid Id { get; }
    string? Ref { get; set; }
}
