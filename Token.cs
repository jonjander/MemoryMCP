namespace MemoryMCP;

public class Token
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public TokenStatus Status { get; set; } = TokenStatus.Active;

    public Guid? SupersedesTokenId { get; set; }

    public Token? SupersedesToken { get; set; }

    public Guid? SupersededByTokenId { get; set; }

    public Token? SupersededByToken { get; set; }

    public ICollection<MemoryToken> Memories { get; set; } = [];

    public int? IntValue { get; set; }

    public bool? BoolValue { get; set; }

    public string? StringValue { get; set; }

    public float? FloatValue { get; set; }

    public DateTime? DateTimeValue { get; set; }

    public string Property { get; set; } = string.Empty;

    public PropertyType Type { get; set; }

    public string SearchValue { get; set; } = string.Empty;

    public float Confidence { get; set; }

    public TokenSource Source { get; set; }

    public ICollection<TokenRevision> Revisions { get; set; } = [];
}

public enum PropertyType
{
    Int,
    Bool,
    String,
    Float,
    DateTime
}

public enum TokenSource
{
    Extracted,
    Derived,
    UserProvided
}

public enum TokenStatus
{
    Active,
    Superseded,
    Deprecated
}
