namespace MemoryMCP.Models;

public record MemoryDetailDto(
    string Ref,
    Guid Id,
    string Raw,
    DateTime Created,
    DateTime? Updated,
    DateTime? MemoryFrom,
    MemoryStatus Status,
    string? StatusNote,
    Guid? SupersedesMemoryId,
    Guid? SupersededByMemoryId,
    IReadOnlyList<EntitySummaryDto> Entities,
    IReadOnlyList<TokenSummaryDto> Tokens,
    IReadOnlyList<RelationshipSummaryDto> Relationships,
    IReadOnlyList<MemoryRevisionDto> Revisions,
    MemorySummaryDto? SupersedesMemory,
    MemorySummaryDto? SupersededByMemory,
    string? Partition = null,
    int RawLength = 0,
    bool RawTruncated = false,
    int RawOffset = 0);

public record MemorySummaryDto(
    string Ref,
    Guid Id,
    string Raw,
    DateTime Created,
    DateTime? MemoryFrom,
    MemoryStatus Status = MemoryStatus.Active,
    Guid? SupersedesMemoryId = null,
    Guid? SupersededByMemoryId = null,
    string? Partition = null,
    int RawLength = 0,
    bool RawTruncated = false);

public enum MemoryListSort
{
    CreatedDesc,
    TokenCountAsc,
    TokenCountDesc
}

public record MemoryListItemDto(
    string Ref,
    Guid Id,
    string Raw,
    DateTime Created,
    DateTime? MemoryFrom,
    MemoryStatus Status,
    int TokenCount,
    int EntityCount,
    Guid? SupersedesMemoryId = null,
    Guid? SupersededByMemoryId = null,
    string? Partition = null,
    int RawLength = 0,
    bool RawTruncated = false);

public record MemoryListFilters(
    int? MaxTokenCount,
    int? MinTokenCount,
    int? ExactTokenCount,
    int? PercentBelowAverage,
    DateTime? CreatedSince,
    bool IncludeInactive,
    MemoryListSort Sort);

public record ListMemoriesResult(
    IReadOnlyList<MemoryListItemDto> Items,
    int Skip,
    int Take,
    int TotalMatching,
    int TotalInScope,
    double AverageTokenCount,
    MemoryListFilters Filters);

public record MemoryRevisionDto(
    Guid Id,
    MemoryRevisionType RevisionType,
    DateTime Created,
    string? Note,
    DateTime? PreviousMemoryFrom,
    DateTime? NewMemoryFrom,
    MemoryStatus? PreviousStatus,
    MemoryStatus? NewStatus,
    Guid? SuccessorMemoryId,
    string? SuccessorRaw);

public record MemoryHistoryDto(
    MemorySummaryDto Memory,
    IReadOnlyList<MemoryRevisionDto> Revisions,
    IReadOnlyList<MemorySummaryDto> CorrectionChain);

public record ExactRawDuplicateWarning(
    string Message,
    int ExistingCount,
    IReadOnlyList<MemorySummaryDto> ExistingMemories);

public record CreateMemoryResult(
    MemorySummaryDto Memory,
    ExactRawDuplicateWarning? ExactRawDuplicateWarning);

public record ReviseMemoryResultDto(
    Guid OriginalMemoryId,
    Guid SuccessorMemoryId,
    MemorySummaryDto Original,
    MemorySummaryDto Successor,
    ExactRawDuplicateWarning? ExactRawDuplicateWarning = null);

public record EntitySummaryDto(
    string Ref,
    Guid Id,
    string Type,
    string Name,
    int MemoryCount,
    EntityStatus Status = EntityStatus.Active,
    Guid? MergedIntoEntityId = null);

public record EntityDetailDto(
    string Ref,
    Guid Id,
    string Type,
    string Name,
    int MemoryCount,
    EntityStatus Status,
    DateTime? Updated,
    Guid? MergedIntoEntityId,
    IReadOnlyList<RelationshipSummaryDto> OutgoingRelationships,
    IReadOnlyList<RelationshipSummaryDto> IncomingRelationships,
    IReadOnlyList<MemorySummaryDto> RecentMemories,
    IReadOnlyList<EntityRevisionDto> Revisions);

public record EntityRevisionDto(
    Guid Id,
    EntityRevisionType RevisionType,
    DateTime Created,
    string? Note,
    string? PreviousName,
    string? NewName,
    string? PreviousType,
    string? NewType,
    EntityStatus? PreviousStatus,
    EntityStatus? NewStatus,
    Guid? RelatedEntityId);

public record EntityHistoryDto(
    EntitySummaryDto Entity,
    IReadOnlyList<EntityRevisionDto> Revisions);

public record MergeEntitiesResultDto(
    Guid SourceEntityId,
    Guid TargetEntityId,
    EntitySummaryDto Source,
    EntitySummaryDto Target,
    int MemoriesMoved,
    int RelationshipsMoved);

public record TokenSummaryDto(
    string Ref,
    Guid Id,
    string Property,
    PropertyType Type,
    string DisplayValue,
    float Confidence,
    TokenSource Source,
    TokenStatus Status = TokenStatus.Active,
    Guid? SupersedesTokenId = null,
    Guid? SupersededByTokenId = null);

public record TokenDetailDto(
    TokenSummaryDto Token,
    DateTime Created,
    DateTime? Updated,
    IReadOnlyList<Guid> LinkedMemoryIds,
    IReadOnlyList<TokenRevisionDto> Revisions,
    TokenSummaryDto? SupersedesToken,
    TokenSummaryDto? SupersededByToken);

public record TokenRevisionDto(
    Guid Id,
    TokenRevisionType RevisionType,
    DateTime Created,
    string? Note,
    string? PreviousProperty,
    string? NewProperty,
    PropertyType? PreviousType,
    PropertyType? NewType,
    string? PreviousSearchValue,
    string? NewSearchValue,
    float? PreviousConfidence,
    float? NewConfidence,
    TokenSource? PreviousSource,
    TokenSource? NewSource,
    TokenStatus? PreviousStatus,
    TokenStatus? NewStatus,
    Guid? SuccessorTokenId);

public record TokenHistoryDto(
    TokenSummaryDto Token,
    IReadOnlyList<TokenRevisionDto> Revisions,
    IReadOnlyList<TokenSummaryDto> CorrectionChain);

public record SupersedeTokenResultDto(
    Guid OriginalTokenId,
    Guid SuccessorTokenId,
    TokenSummaryDto Original,
    TokenSummaryDto Successor,
    IReadOnlyList<Guid> RelinkedMemoryIds);

public record PropertyCatalogEntryDto(
    string Property,
    int ActiveTokenCount,
    int TotalTokenCount,
    IReadOnlyList<PropertyType> Types,
    IReadOnlyList<string> SampleValues);

public record TokenUsageEntryDto(
    string Ref,
    Guid Id,
    string DisplayValue,
    PropertyType Type,
    int MemoryLinkCount,
    TokenStatus Status);

public record PropertyTokenGroupDto(
    string Property,
    int ActiveTokenCount,
    int TotalTokenCount,
    int TotalMemoryLinks,
    IReadOnlyList<PropertyType> Types,
    IReadOnlyList<TokenUsageEntryDto> Tokens);

public record RenamePropertyResultDto(
    string FromProperty,
    string ToProperty,
    int TokensUpdated,
    IReadOnlyList<Guid> UpdatedTokenIds,
    bool Preview = false);

public record MergePropertiesResultDto(
    string ToProperty,
    IReadOnlyList<RenamePropertyResultDto> Renames,
    int TotalTokensUpdated,
    bool Preview = false);

public record SplitTokenResultDto(
    Guid OriginalTokenId,
    string OriginalProperty,
    string TargetProperty,
    IReadOnlyList<string> Parts,
    IReadOnlyList<TokenSummaryDto> NewTokens,
    TokenSummaryDto? DeprecatedOriginal,
    bool Preview = false);

public record RelationshipSummaryDto(
    Guid Id,
    Guid FromEntityId,
    string FromEntityName,
    Guid ToEntityId,
    string ToEntityName,
    string RelationType,
    float Confidence,
    Guid? MemoryId);

public record EntityGraphDto(
    EntityDetailDto Entity,
    IReadOnlyList<RelationshipSummaryDto> OutgoingRelationships,
    IReadOnlyList<RelationshipSummaryDto> IncomingRelationships,
    IReadOnlyList<MemorySummaryDto> RecentMemories);

public record BundleEntityInput(
    string ClientKey,
    string Type,
    string Name,
    bool ForceCreate = false);

public record BundleTokenInput(
    string Property,
    PropertyType Type,
    int? IntValue = null,
    bool? BoolValue = null,
    string? StringValue = null,
    float? FloatValue = null,
    DateTime? DateTimeValue = null,
    float Confidence = 1f,
    TokenSource Source = TokenSource.Extracted);

public record BundleRelationshipInput(
    string FromClientKey,
    string ToClientKey,
    string RelationType,
    float Confidence = 1f);

public record StoreMemoryBundleInput(
    string Raw,
    DateTime? MemoryFrom = null,
    IReadOnlyList<BundleEntityInput>? Entities = null,
    IReadOnlyList<BundleTokenInput>? Tokens = null,
    IReadOnlyList<string>? EntityLinks = null,
    IReadOnlyList<BundleRelationshipInput>? Relationships = null,
    bool ReuseTokens = true);

public record StoreMemoryBundleResult(
    string MemoryRef,
    Guid MemoryId,
    IReadOnlyDictionary<string, string> EntityRefs,
    IReadOnlyDictionary<string, Guid> EntityIds,
    IReadOnlyList<string> TokenRefs,
    IReadOnlyList<Guid> TokenIds,
    IReadOnlyList<Guid> RelationshipIds,
    ExactRawDuplicateWarning? ExactRawDuplicateWarning = null);

public record StoreMemoryBundleBatchItemResult(
    int Index,
    StoreMemoryBundleResult Result);

public record StoreMemoryBundlesResult(
    int Count,
    IReadOnlyList<StoreMemoryBundleBatchItemResult> Results);

public record MemoryTokenLinkInput(
    string MemoryId,
    string TokenId);

public record LinkMemoryTokensResult(
    int Requested,
    int Linked,
    int Skipped,
    IReadOnlyList<MemoryTokenLinkResolved> Links);

public record MemoryTokenLinkResolved(
    string MemoryRef,
    Guid MemoryId,
    string TokenRef,
    Guid TokenId);

public record CreateAndLinkTokenInput(
    string MemoryId,
    string Property,
    PropertyType Type,
    int? IntValue = null,
    bool? BoolValue = null,
    string? StringValue = null,
    float? FloatValue = null,
    DateTime? DateTimeValue = null,
    float Confidence = 1f,
    TokenSource Source = TokenSource.Extracted,
    bool ReuseToken = true);

public record CreateAndLinkTokenResultItem(
    string MemoryRef,
    Guid MemoryId,
    string TokenRef,
    Guid TokenId,
    TokenSummaryDto Token);

public record CreateAndLinkTokensResult(
    int Count,
    IReadOnlyList<CreateAndLinkTokenResultItem> Results);

public record SqliteImportOptions(
    IReadOnlyList<string> SourcePaths,
    bool ReuseEntities = true,
    bool ReuseTokens = true,
    bool SkipDuplicateRaw = true);

public record SqliteImportTotals(
    int EntitiesReused,
    int EntitiesNew,
    int TokensReused,
    int TokensNew,
    int MemoriesImported,
    int MemoriesSkippedDuplicateRaw,
    int RelationshipsImported,
    int RelationshipsSkipped);

public record SqliteImportFilePreview(
    string SourcePath,
    int EntitiesReused,
    int EntitiesNew,
    int MergedEntitiesInSource,
    int TokensReused,
    int TokensNew,
    int MemoriesImported,
    int MemoriesSkippedDuplicateRaw,
    int RelationshipsImported,
    int RelationshipsSkipped,
    IReadOnlyList<string> Warnings);

public record SqliteImportPreviewResult(
    IReadOnlyList<SqliteImportFilePreview> Files,
    SqliteImportTotals Totals);

public record SqliteImportFileResult(
    string SourcePath,
    int EntitiesReused,
    int EntitiesNew,
    int TokensReused,
    int TokensNew,
    int MemoriesImported,
    int MemoriesSkippedDuplicateRaw,
    int RelationshipsImported,
    int RelationshipsSkipped);

public record SqliteImportResult(
    IReadOnlyList<SqliteImportFileResult> Files,
    SqliteImportTotals Totals);
