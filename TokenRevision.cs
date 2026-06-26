namespace MemoryMCP;

public class TokenRevision
{
    public Guid Id { get; set; }

    public Guid TokenId { get; set; }

    public Token Token { get; set; } = null!;

    public TokenRevisionType RevisionType { get; set; }

    public DateTime Created { get; set; }

    public string? Note { get; set; }

    public string? PreviousProperty { get; set; }

    public string? NewProperty { get; set; }

    public PropertyType? PreviousType { get; set; }

    public PropertyType? NewType { get; set; }

    public string? PreviousSearchValue { get; set; }

    public string? NewSearchValue { get; set; }

    public float? PreviousConfidence { get; set; }

    public float? NewConfidence { get; set; }

    public TokenSource? PreviousSource { get; set; }

    public TokenSource? NewSource { get; set; }

    public TokenStatus? PreviousStatus { get; set; }

    public TokenStatus? NewStatus { get; set; }

    public Guid? SuccessorTokenId { get; set; }
}

public enum TokenRevisionType
{
    PropertyUpdated,
    TypeUpdated,
    ValueUpdated,
    ConfidenceUpdated,
    SourceUpdated,
    Superseded,
    Deprecated,
    Reactivated
}
