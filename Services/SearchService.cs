using MemoryMCP.Data;
using MemoryMCP.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Services;

public class SearchService(MemoryDbContext db, RefIdResolver refResolver)
{
    public async Task<IReadOnlyList<MemorySummaryDto>> SearchMemoriesByTextAsync(
        string query,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var trimmed = query.Trim();

        if (!db.Database.IsSqlite())
        {
            try
            {
                List<Memory> ftsResults;
                if (includeInactive)
                {
                    ftsResults = await db.Memories
                        .FromSqlInterpolated($"SELECT * FROM [Memories] WHERE FREETEXT([Raw], {trimmed})")
                        .AsNoTracking()
                        .OrderByDescending(m => m.Created)
                        .Take(100)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    ftsResults = await db.Memories
                        .FromSqlInterpolated($"SELECT * FROM [Memories] WHERE FREETEXT([Raw], {trimmed}) AND [Status] = {(int)MemoryStatus.Active}")
                        .AsNoTracking()
                        .OrderByDescending(m => m.Created)
                        .Take(100)
                        .ToListAsync(cancellationToken);
                }

                if (ftsResults.Count > 0)
                    return ftsResults.Select(ToSummary).ToList();
            }
            catch
            {
                // Fall back to LIKE when full-text search is unavailable.
            }
        }

        var likePattern = $"%{trimmed}%";
        var likeResults = await db.Memories
            .AsNoTracking()
            .WhereActive(includeInactive)
            .Where(m => EF.Functions.Like(m.Raw, likePattern))
            .OrderByDescending(m => m.Created)
            .Take(100)
            .ToListAsync(cancellationToken);

        return likeResults.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<MemorySummaryDto>> SearchMemoriesByEntityAsync(
        string? entityId = null,
        string? entityName = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MemoryEntity> query = db.MemoryEntities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            var resolvedId = await refResolver.ResolveEntityIdAsync(entityId, cancellationToken);
            query = query.Where(me => me.EntityId == resolvedId);
        }
        else if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(me => me.Entity.Name.Contains(entityName) && me.Entity.Status == EntityStatus.Active);
        else
            return [];

        var memories = await query
            .Select(me => me.Memory)
            .Where(m => includeInactive || m.Status == MemoryStatus.Active)
            .Distinct()
            .OrderByDescending(m => m.Created)
            .Take(100)
            .ToListAsync(cancellationToken);

        return memories.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<MemorySummaryDto>> SearchMemoriesByTokenAsync(
        string property,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var tokenQuery = db.Tokens.AsNoTracking().Where(t => t.Property == property && t.Status == TokenStatus.Active);

        if (type.HasValue)
            tokenQuery = tokenQuery.Where(t => t.Type == type.Value);
        if (intValue.HasValue)
            tokenQuery = tokenQuery.Where(t => t.IntValue == intValue.Value);
        if (boolValue.HasValue)
            tokenQuery = tokenQuery.Where(t => t.BoolValue == boolValue.Value);
        if (!string.IsNullOrWhiteSpace(stringValue))
        {
            var sv = stringValue.Trim().ToLowerInvariant();
            tokenQuery = tokenQuery.Where(t => t.SearchValue == sv);
        }
        if (floatValue.HasValue)
            tokenQuery = tokenQuery.Where(t => t.FloatValue == floatValue.Value);
        if (dateTimeValue.HasValue)
            tokenQuery = tokenQuery.Where(t => t.DateTimeValue == dateTimeValue.Value);

        var memories = await db.MemoryTokens
            .AsNoTracking()
            .Where(mt => tokenQuery.Select(t => t.Id).Contains(mt.TokenId))
            .Select(mt => mt.Memory)
            .Where(m => includeInactive || m.Status == MemoryStatus.Active)
            .Distinct()
            .OrderByDescending(m => m.Created)
            .Take(100)
            .ToListAsync(cancellationToken);

        return memories.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<EntitySummaryDto>> SearchEntitiesByTokenAsync(
        string property,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        CancellationToken cancellationToken = default)
    {
        var tokenQuery = db.Tokens.AsNoTracking().Where(t => t.Property == property && t.Status == TokenStatus.Active);

        if (type.HasValue)
            tokenQuery = tokenQuery.Where(t => t.Type == type.Value);
        if (intValue.HasValue)
            tokenQuery = tokenQuery.Where(t => t.IntValue == intValue.Value);
        if (boolValue.HasValue)
            tokenQuery = tokenQuery.Where(t => t.BoolValue == boolValue.Value);
        if (!string.IsNullOrWhiteSpace(stringValue))
        {
            var sv = stringValue.Trim().ToLowerInvariant();
            tokenQuery = tokenQuery.Where(t => t.SearchValue == sv);
        }
        if (floatValue.HasValue)
            tokenQuery = tokenQuery.Where(t => t.FloatValue == floatValue.Value);
        if (dateTimeValue.HasValue)
            tokenQuery = tokenQuery.Where(t => t.DateTimeValue == dateTimeValue.Value);

        var tokenIds = tokenQuery.Select(t => t.Id);

        var entities = await db.MemoryEntities
            .AsNoTracking()
            .Where(me => db.MemoryTokens.Any(mt => mt.MemoryId == me.MemoryId && tokenIds.Contains(mt.TokenId)))
            .Where(me => me.Memory.Status == MemoryStatus.Active)
            .Where(me => me.Entity.Status == EntityStatus.Active)
            .Select(me => me.Entity)
            .Distinct()
            .Select(e => ModelMappers.ToSummary(e, e.Memories.Count))
            .OrderBy(e => e.Name)
            .Take(100)
            .ToListAsync(cancellationToken);

        return entities;
    }

    private static MemorySummaryDto ToSummary(Memory memory) => ModelMappers.ToSummary(memory);
}
