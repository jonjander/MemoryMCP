using MemoryMCP.Models;

namespace MemoryMCP.Services;

public static class ModelMappers
{
    public static EntitySummaryDto ToSummary(Entity entity, int memoryCount = 0) =>
        new(entity.Ref ?? string.Empty, entity.Id, entity.Type, entity.Name, memoryCount, entity.Status, entity.MergedIntoEntityId);

    public static MemorySummaryDto ToSummary(Memory memory) =>
        new(
            memory.Ref ?? string.Empty,
            memory.Id,
            memory.Raw,
            memory.Created,
            memory.MemoryFrom,
            memory.Status,
            memory.SupersedesMemoryId,
            memory.SupersededByMemoryId);

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
