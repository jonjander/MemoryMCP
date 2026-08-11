using MemoryMCP.Models;

namespace MemoryMCP.Services;

public static class ModelMappers
{
    public static EntitySummaryDto ToSummary(Entity entity, int memoryCount = 0) =>
        new(entity.Ref ?? string.Empty, entity.Id, entity.Type, entity.Name, memoryCount, entity.Status, entity.MergedIntoEntityId);

    public static MemorySummaryDto ToSummary(Memory memory) =>
        ToSummaryPreview(memory);

    public static MemorySummaryDto ToSummaryPreview(Memory memory, int previewChars = MemoryLimits.PreviewChars)
    {
        var (preview, length, truncated) = RawTextHelper.ToPreview(memory.Raw, previewChars);
        return new MemorySummaryDto(
            memory.Ref ?? string.Empty,
            memory.Id,
            preview,
            memory.Created,
            memory.MemoryFrom,
            memory.Status,
            memory.SupersedesMemoryId,
            memory.SupersededByMemoryId,
            memory.Partition,
            length,
            truncated);
    }

    public static MemoryListItemDto ToListItem(Memory memory, int tokenCount, int entityCount) =>
        ToListItemPreview(memory, tokenCount, entityCount);

    public static MemoryListItemDto ToListItemPreview(Memory memory, int tokenCount, int entityCount, int previewChars = MemoryLimits.PreviewChars)
    {
        var (preview, length, truncated) = RawTextHelper.ToPreview(memory.Raw, previewChars);
        return new MemoryListItemDto(
            memory.Ref ?? string.Empty,
            memory.Id,
            preview,
            memory.Created,
            memory.MemoryFrom,
            memory.Status,
            tokenCount,
            entityCount,
            memory.SupersedesMemoryId,
            memory.SupersededByMemoryId,
            memory.Partition,
            length,
            truncated);
    }

    public static TokenSummaryDto ToSummary(Token token) =>
        new(
            token.Ref ?? string.Empty,
            token.Id,
            token.Property,
            token.Type,
            TokenValueHelper.FormatDisplayValue(token),
            token.Confidence,
            token.Source,
            token.Status,
            token.SupersedesTokenId,
            token.SupersededByTokenId);
}
