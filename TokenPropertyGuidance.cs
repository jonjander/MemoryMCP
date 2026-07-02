namespace MemoryMCP;

/// <summary>
/// Shared guidance for MCP tool descriptions about how token Property names should be used.
/// </summary>
public static class TokenPropertyGuidance
{
    public const string PropertyParameter =
        "Abstract, reusable facet name shared across unrelated memories and entities — NOT entity-specific. " +
        "Good: Year, Name, Likes, Color, Age. Bad: SofiaBirthYear, WineBoffAge, LikesFood. " +
        "Identity lives on entities; values live on tokens. Prefer one token per atomic value " +
        "(two wines → two Name tokens + two Year tokens, not BoffName and FooName). " +
        "Vocabulary emerges over time — discover and converge via list_token_properties and maintenance tools.";

    public const string EntityIdentityGuidance =
        "Do not create Name tokens for people or systems already in entityLinks — their identity belongs on the entity. " +
        "Name tokens are for facet values stated in raw text (e.g. wine name 'böff'), not for entity disambiguation.";

    public const string ProvenanceGuidance =
        "Add a token only when the value is stated or directly extractable from raw text for a linked entity. " +
        "source=Extracted: verbatim or obvious reading of raw. source=Derived: computed from raw (e.g. birth year from age + memoryFrom). " +
        "Do NOT attach entity B the same facet value as entity A when raw only says they are alike " +
        "(e.g. 'same age as Sandra' → relationship SameAgeAs to Sandra, not Age=38 on Åsa). " +
        "Confidence and source are stored on the token; reuseTokens merges by property+type+value only — " +
        "never reuse a shared token to represent a weaker inferred fact.";

    public const string ReuseTokensGuidance =
        "reuseTokens=true (default): link to an existing token when another memory independently states the same " +
        "property+type+value (mesh search: two unrelated memories both say someone is 38 → one Age=38 token, two memory links; " +
        "who is 38 is resolved via entityLinks on each memory). reuseTokens=false when the same numeric/string value " +
        "would misrepresent provenance (inferred vs stated) or when facts should stay isolated.";

    public const string RelationshipsVsTokensGuidance =
        "relationshipsJson: facts about how entities relate — SameAgeAs, ParentOf, FriendOf, ColleagueOf, etc. " +
        "Prefer a relationship when raw text expresses comparison, kinship, or linkage rather than a standalone facet. " +
        "Traverse relationships to answer inferred questions; tokens index what was directly observed.";

    public const string AgeVsYearGuidance =
        "Prefer Year (birth/foundation year) when stated or stable over time. Use Age only when raw gives age, not birth year. " +
        "Age drifts — set memoryFrom when the observation date matters.";

    public const string BundleTokensJson =
        "JSON array of abstract tokens linked to this memory. Each item is one atomic fact: " +
        "[{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":1988,\"confidence\":0.95,\"source\":\"Extracted\"}]. " +
        "Use generic property names (Year, Likes, Age) — never prefix with entity names. " +
        "Multiple values of the same property in one memory are fine (two Year tokens for two wines). " +
        "Split compound values into separate tokens (Likes=pasta and Likes=hamburgare, not LikesFood=\"pasta, hamburgare\"). " +
        EntityIdentityGuidance + " " + ProvenanceGuidance;

    public const string CreateTokenDescription =
        "Create one atomic structured fact for fast cross-memory search and mesh queries. " +
        PropertyParameter + " " + ProvenanceGuidance;

    public const string StoreBundleDescription =
        "Store a memory with entities, tokens, and relationships in one transaction. " +
        "Response includes memoryRef, entityRefs, tokenRefs (prefer Ref in follow-ups; Guid also returned). " +
        "Use when the user explicitly asks to save ('spara i minnet', 'kom ihåg') or after they confirm they want a fact in memory. " +
        "Do NOT ask about entity structure — infer entities and tokens from the text. " +
        "Use abstract token properties; link entities via entityLinks. " +
        "When a memory mentions multiple subjects, still use shared property names and multiple tokens — " +
        "or split into one memory per subject if facts would be ambiguous. " +
        ProvenanceGuidance + " " + RelationshipsVsTokensGuidance + " " + AgeVsYearGuidance;

    public const string ListPropertiesDescription =
        "List distinct token property names in the knowledge base with counts and sample values. " +
        "Use when storing or retrieving memories to discover existing vocabulary and spot overly specific properties.";

    public const string ListTokensByPropertyDescription =
        "List tokens grouped by property with per-token memory link counts. " +
        "Properties are ordered by total usage (popular first); tokens within each group by fewest links first. " +
        "Use to spot popular facets (Year, Likes) vs rare one-off properties and values worth merging, renaming, or splitting.";

    public const string RenamePropertyDescription =
        "Rename a token property across all active tokens (e.g. SofiaBirthYear → Year, BirthYear → Year). " +
        "Use during maintenance when properties became too entity-specific. Set preview=true to inspect impact first.";

    public const string MergePropertiesDescription =
        "Rename several property names into one canonical name (e.g. [\"SofiaBirthYear\",\"JonBirthYear\",\"BirthYear\"] → Year). " +
        "Equivalent to multiple rename_token_property calls. Set preview=true to inspect first.";

    public const string SplitTokenDescription =
        "Split one string token with multiple values into separate atomic tokens (e.g. LikesFood=\"pasta, hamburgare\" → two Likes tokens). " +
        "Relinks the same memories. Deprecates the original. Set preview=true to inspect parts first.";
}
