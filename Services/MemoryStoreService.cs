using MemoryMCP.Data;
using MemoryMCP.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Services;

public class MemoryStoreService(MemoryDbContext db, RefIdResolver refResolver)
{
    public async Task<MemorySummaryDto> CreateMemoryAsync(string raw, DateTime? memoryFrom = null, CancellationToken cancellationToken = default)
    {
        var memory = new Memory
        {
            Id = Guid.NewGuid(),
            Raw = raw,
            Created = DateTime.UtcNow,
            MemoryFrom = memoryFrom,
            Status = MemoryStatus.Active
        };

        db.Memories.Add(memory);
        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(memory);
    }

    public async Task<MemoryDetailDto?> GetMemoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var memory = await db.Memories
            .AsNoTracking()
            .Include(m => m.Entities).ThenInclude(me => me.Entity)
            .Include(m => m.Tokens).ThenInclude(mt => mt.Token)
            .Include(m => m.Revisions)
            .Include(m => m.SupersedesMemory)
            .Include(m => m.SupersededByMemory)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (memory is null)
            return null;

        var entityIds = memory.Entities.Select(me => me.EntityId).ToList();
        var relationships = await db.EntityRelationships
            .AsNoTracking()
            .Include(r => r.FromEntity)
            .Include(r => r.ToEntity)
            .Where(r => r.MemoryId == id || entityIds.Contains(r.FromEntityId) || entityIds.Contains(r.ToEntityId))
            .ToListAsync(cancellationToken);

        return ToDetail(memory, relationships);
    }

    public async Task<IReadOnlyList<MemorySummaryDto>> ListMemoriesAsync(
        int skip = 0,
        int take = 50,
        DateTime? createdSince = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        skip = Math.Max(skip, 0);

        var query = db.Memories.AsNoTracking().WhereActive(includeInactive);
        if (createdSince.HasValue)
            query = query.Where(m => m.Created >= createdSince.Value);

        var memories = await query
            .OrderByDescending(m => m.Created)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return memories.Select(ToSummary).ToList();
    }

    public async Task<MemorySummaryDto> UpdateMemoryFromAsync(
        Guid id,
        DateTime memoryFrom,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var memory = await db.Memories.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Memory not found.");

        EnsureMutable(memory);

        var previous = memory.MemoryFrom;
        memory.MemoryFrom = memoryFrom;
        memory.Updated = DateTime.UtcNow;

        await AddRevisionAsync(
            memory,
            MemoryRevisionType.MemoryFromUpdated,
            note,
            previous,
            memoryFrom,
            memory.Status,
            memory.Status,
            null,
            null,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(memory);
    }

    public async Task<MemorySummaryDto> InvalidateMemoryAsync(
        Guid id,
        MemoryStatus status,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (status is not (MemoryStatus.Invalid or MemoryStatus.Retracted))
            throw new InvalidOperationException("Status must be Invalid or Retracted.");

        var memory = await db.Memories.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Memory not found.");

        if (memory.Status == MemoryStatus.Superseded)
            throw new InvalidOperationException("Cannot invalidate a superseded memory. Use the active successor instead.");

        var previousStatus = memory.Status;
        memory.Status = status;
        memory.StatusNote = note;
        memory.Updated = DateTime.UtcNow;

        await AddRevisionAsync(
            memory,
            status == MemoryStatus.Invalid ? MemoryRevisionType.Invalidated : MemoryRevisionType.Retracted,
            note,
            memory.MemoryFrom,
            memory.MemoryFrom,
            previousStatus,
            status,
            null,
            null,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(memory);
    }

    public async Task<ReviseMemoryResultDto> ReviseMemoryAsync(
        Guid originalId,
        string correctedRaw,
        DateTime? memoryFrom = null,
        string? note = null,
        bool copyEntityLinks = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correctedRaw))
            throw new InvalidOperationException("Corrected raw text is required.");

        var original = await db.Memories
            .Include(m => m.Entities)
            .FirstOrDefaultAsync(m => m.Id == originalId, cancellationToken)
            ?? throw new InvalidOperationException("Memory not found.");

        if (original.Status == MemoryStatus.Superseded)
            throw new InvalidOperationException("Memory is already superseded.");

        var successor = new Memory
        {
            Id = Guid.NewGuid(),
            Raw = correctedRaw,
            Created = DateTime.UtcNow,
            MemoryFrom = memoryFrom ?? original.MemoryFrom,
            Status = MemoryStatus.Active,
            SupersedesMemoryId = original.Id,
            StatusNote = note
        };

        if (copyEntityLinks)
        {
            foreach (var link in original.Entities)
            {
                successor.Entities.Add(new MemoryEntity
                {
                    MemoryId = successor.Id,
                    EntityId = link.EntityId
                });
            }
        }

        original.Status = MemoryStatus.Superseded;
        original.SupersededByMemoryId = successor.Id;
        original.StatusNote = note;
        original.Updated = DateTime.UtcNow;

        db.Memories.Add(successor);

        await AddRevisionAsync(
            original,
            MemoryRevisionType.Superseded,
            note,
            original.MemoryFrom,
            original.MemoryFrom,
            MemoryStatus.Active,
            MemoryStatus.Superseded,
            successor.Id,
            correctedRaw,
            cancellationToken);

        await AddRevisionAsync(
            successor,
            MemoryRevisionType.Corrected,
            note,
            null,
            successor.MemoryFrom,
            null,
            MemoryStatus.Active,
            null,
            null,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new ReviseMemoryResultDto(
            original.Id,
            successor.Id,
            ToSummary(original),
            ToSummary(successor));
    }

    public async Task<MemoryHistoryDto?> GetMemoryHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var memory = await db.Memories
            .AsNoTracking()
            .Include(m => m.Revisions)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (memory is null)
            return null;

        var chain = new List<MemorySummaryDto> { ToSummary(memory) };
        var current = memory;

        while (current.SupersedesMemoryId.HasValue)
        {
            var predecessor = await db.Memories.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == current.SupersedesMemoryId.Value, cancellationToken);
            if (predecessor is null)
                break;
            chain.Insert(0, ToSummary(predecessor));
            current = predecessor;
        }

        current = memory;
        while (current.SupersededByMemoryId.HasValue)
        {
            var successor = await db.Memories.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == current.SupersededByMemoryId.Value, cancellationToken);
            if (successor is null)
                break;
            chain.Add(ToSummary(successor));
            current = successor;
        }

        return new MemoryHistoryDto(
            ToSummary(memory),
            memory.Revisions.OrderByDescending(r => r.Created).Select(ToRevisionDto).ToList(),
            chain);
    }

    public async Task LinkMemoryEntityAsync(Guid memoryId, Guid entityId, CancellationToken cancellationToken = default)
    {
        var exists = await db.MemoryEntities.AnyAsync(me => me.MemoryId == memoryId && me.EntityId == entityId, cancellationToken);
        if (exists)
            return;

        var memoryExists = await db.Memories.AnyAsync(m => m.Id == memoryId, cancellationToken);
        var entityExists = await db.Entities.AnyAsync(e => e.Id == entityId, cancellationToken);
        if (!memoryExists || !entityExists)
            throw new InvalidOperationException("Memory or entity not found.");

        db.MemoryEntities.Add(new MemoryEntity { MemoryId = memoryId, EntityId = entityId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkMemoryTokenAsync(Guid memoryId, Guid tokenId, CancellationToken cancellationToken = default)
    {
        var exists = await db.MemoryTokens.AnyAsync(mt => mt.MemoryId == memoryId && mt.TokenId == tokenId, cancellationToken);
        if (exists)
            return;

        var memoryExists = await db.Memories.AnyAsync(m => m.Id == memoryId, cancellationToken);
        var tokenExists = await db.Tokens.AnyAsync(t => t.Id == tokenId, cancellationToken);
        if (!memoryExists || !tokenExists)
            throw new InvalidOperationException("Memory or token not found.");

        db.MemoryTokens.Add(new MemoryToken
        {
            Id = Guid.NewGuid(),
            MemoryId = memoryId,
            TokenId = tokenId
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkMemoryEntityAsync(Guid memoryId, Guid entityId, CancellationToken cancellationToken = default)
    {
        var link = await db.MemoryEntities.FirstOrDefaultAsync(me => me.MemoryId == memoryId && me.EntityId == entityId, cancellationToken);
        if (link is null)
            return;

        db.MemoryEntities.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkMemoryTokenAsync(Guid memoryId, Guid tokenId, CancellationToken cancellationToken = default)
    {
        var link = await db.MemoryTokens.FirstOrDefaultAsync(mt => mt.MemoryId == memoryId && mt.TokenId == tokenId, cancellationToken);
        if (link is null)
            return;

        db.MemoryTokens.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
    }

    public const int MaxBundleBatchSize = 100;
    public const int MaxTokenLinkBatchSize = 500;

    public async Task<StoreMemoryBundleResult> StoreBundleAsync(StoreMemoryBundleInput input, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var core = await StoreBundleCoreAsync(input, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var result = await BuildBundleResultAsync(core, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<StoreMemoryBundlesResult> StoreBundlesAsync(
        IReadOnlyList<StoreMemoryBundleInput> bundles,
        CancellationToken cancellationToken = default)
    {
        if (bundles.Count == 0)
            throw new InvalidOperationException("At least one bundle is required.");

        if (bundles.Count > MaxBundleBatchSize)
            throw new InvalidOperationException($"Batch size {bundles.Count} exceeds maximum of {MaxBundleBatchSize}.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var results = new List<StoreMemoryBundleBatchItemResult>(bundles.Count);
        var cores = new List<BundleCoreResult>(bundles.Count);
        for (var i = 0; i < bundles.Count; i++)
        {
            try
            {
                var core = await StoreBundleCoreAsync(bundles[i], cancellationToken);
                cores.Add(core);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Bundle at index {i} failed: {ex.Message}", ex);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        for (var i = 0; i < cores.Count; i++)
        {
            var result = await BuildBundleResultAsync(cores[i], cancellationToken);
            results.Add(new StoreMemoryBundleBatchItemResult(i, result));
        }

        await transaction.CommitAsync(cancellationToken);
        return new StoreMemoryBundlesResult(results.Count, results);
    }

    private async Task<StoreMemoryBundleResult> BuildBundleResultAsync(BundleCoreResult core, CancellationToken cancellationToken)
    {
        var memory = await db.Memories.AsNoTracking().FirstAsync(m => m.Id == core.MemoryId, cancellationToken);

        var entityGuidList = core.EntityIds.Values.Distinct().ToList();
        var entities = entityGuidList.Count == 0
            ? new Dictionary<Guid, Entity>()
            : await db.Entities.AsNoTracking()
                .Where(e => entityGuidList.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, cancellationToken);

        var entityRefs = core.EntityIds.ToDictionary(
            kv => kv.Key,
            kv => entities[kv.Value].Ref ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var tokens = core.TokenIds.Count == 0
            ? new Dictionary<Guid, Token>()
            : await db.Tokens.AsNoTracking()
                .Where(t => core.TokenIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

        var tokenRefs = core.TokenIds.Select(id => tokens[id].Ref ?? string.Empty).ToList();

        return new StoreMemoryBundleResult(
            memory.Ref ?? string.Empty,
            memory.Id,
            entityRefs,
            core.EntityIds,
            tokenRefs,
            core.TokenIds,
            core.RelationshipIds);
    }

    private sealed record BundleCoreResult(
        Guid MemoryId,
        IReadOnlyDictionary<string, Guid> EntityIds,
        IReadOnlyList<Guid> TokenIds,
        IReadOnlyList<Guid> RelationshipIds);

    public async Task<LinkMemoryTokensResult> LinkMemoryTokensAsync(
        IReadOnlyList<MemoryTokenLinkInput> links,
        CancellationToken cancellationToken = default)
    {
        if (links.Count == 0)
            throw new InvalidOperationException("At least one link is required.");

        if (links.Count > MaxTokenLinkBatchSize)
            throw new InvalidOperationException($"Batch size {links.Count} exceeds maximum of {MaxTokenLinkBatchSize}.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var processed = new List<(Guid MemoryId, Guid TokenId)>();
        var newlyLinked = 0;
        var skipped = 0;

        foreach (var link in links)
        {
            var memoryId = await refResolver.ResolveMemoryIdAsync(link.MemoryId, cancellationToken);
            var tokenId = await refResolver.ResolveTokenIdAsync(link.TokenId, cancellationToken);

            var exists = await db.MemoryTokens.AnyAsync(
                mt => mt.MemoryId == memoryId && mt.TokenId == tokenId,
                cancellationToken);

            if (exists)
            {
                skipped++;
                processed.Add((memoryId, tokenId));
                continue;
            }

            var memoryExists = await db.Memories.AnyAsync(m => m.Id == memoryId, cancellationToken);
            var tokenExists = await db.Tokens.AnyAsync(t => t.Id == tokenId, cancellationToken);
            if (!memoryExists || !tokenExists)
                throw new InvalidOperationException($"Memory or token not found for link memoryId={link.MemoryId}, tokenId={link.TokenId}.");

            db.MemoryTokens.Add(new MemoryToken
            {
                Id = Guid.NewGuid(),
                MemoryId = memoryId,
                TokenId = tokenId
            });
            newlyLinked++;
            processed.Add((memoryId, tokenId));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var resolved = await ResolveLinkRefsAsync(processed, cancellationToken);
        return new LinkMemoryTokensResult(links.Count, newlyLinked, skipped, resolved);
    }

    private async Task<IReadOnlyList<MemoryTokenLinkResolved>> ResolveLinkRefsAsync(
        IReadOnlyList<(Guid MemoryId, Guid TokenId)> links,
        CancellationToken cancellationToken)
    {
        if (links.Count == 0)
            return [];

        var memoryIds = links.Select(l => l.MemoryId).Distinct().ToList();
        var tokenIds = links.Select(l => l.TokenId).Distinct().ToList();

        var memories = await db.Memories.AsNoTracking()
            .Where(m => memoryIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);
        var tokens = await db.Tokens.AsNoTracking()
            .Where(t => tokenIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return links
            .Select(l => new MemoryTokenLinkResolved(
                memories[l.MemoryId].Ref ?? string.Empty,
                l.MemoryId,
                tokens[l.TokenId].Ref ?? string.Empty,
                l.TokenId))
            .ToList();
    }

    public async Task<CreateAndLinkTokensResult> CreateAndLinkTokensAsync(
        IReadOnlyList<CreateAndLinkTokenInput> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("At least one token item is required.");

        if (items.Count > MaxTokenLinkBatchSize)
            throw new InvalidOperationException($"Batch size {items.Count} exceeds maximum of {MaxTokenLinkBatchSize}.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var tokenService = new TokenService(db);
        var results = new List<CreateAndLinkTokenResultItem>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            try
            {
                var memoryId = await refResolver.ResolveMemoryIdAsync(item.MemoryId, cancellationToken);
                var memoryExists = await db.Memories.AnyAsync(m => m.Id == memoryId, cancellationToken);
                if (!memoryExists)
                    throw new InvalidOperationException($"Memory not found: {item.MemoryId}.");

                var tokenInput = new BundleTokenInput(
                    item.Property,
                    item.Type,
                    item.IntValue,
                    item.BoolValue,
                    item.StringValue,
                    item.FloatValue,
                    item.DateTimeValue,
                    item.Confidence,
                    item.Source);

                var token = item.ReuseToken
                    ? await tokenService.FindOrCreateAsync(tokenInput, cancellationToken)
                    : await tokenService.CreateAsync(tokenInput, cancellationToken);

                var linkExists = await db.MemoryTokens.AnyAsync(
                    mt => mt.MemoryId == memoryId && mt.TokenId == token.Id,
                    cancellationToken);

                if (!linkExists)
                {
                    db.MemoryTokens.Add(new MemoryToken
                    {
                        Id = Guid.NewGuid(),
                        MemoryId = memoryId,
                        TokenId = token.Id
                    });
                }

                results.Add(new CreateAndLinkTokenResultItem(
                    string.Empty,
                    memoryId,
                    string.Empty,
                    token.Id,
                    TokenValueHelper.ToSummary(token)));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Token item at index {i} failed: {ex.Message}", ex);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < results.Count; i++)
        {
            var item = results[i];
            var memory = await db.Memories.AsNoTracking().FirstAsync(m => m.Id == item.MemoryId, cancellationToken);
            var token = await db.Tokens.AsNoTracking().FirstAsync(t => t.Id == item.TokenId, cancellationToken);
            results[i] = new CreateAndLinkTokenResultItem(memory.Ref ?? string.Empty, item.MemoryId, token.Ref ?? string.Empty, item.TokenId, item.Token);
        }

        await transaction.CommitAsync(cancellationToken);
        return new CreateAndLinkTokensResult(results.Count, results);
    }

    private async Task<BundleCoreResult> StoreBundleCoreAsync(
        StoreMemoryBundleInput input,
        CancellationToken cancellationToken)
    {
        var memory = new Memory
        {
            Id = Guid.NewGuid(),
            Raw = input.Raw,
            Created = DateTime.UtcNow,
            MemoryFrom = input.MemoryFrom,
            Status = MemoryStatus.Active
        };
        db.Memories.Add(memory);

        var entityResolution = new EntityResolutionService(db);
        var tokenService = new TokenService(db);
        var relationshipService = new RelationshipService(db);

        var entityIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var entityInput in input.Entities ?? [])
        {
            var entity = await entityResolution.ResolveOrCreateAsync(entityInput.Type, entityInput.Name, entityInput.ForceCreate, cancellationToken);
            entityIds[entityInput.ClientKey] = entity.Id;
        }

        foreach (var clientKey in input.EntityLinks ?? [])
        {
            if (!entityIds.TryGetValue(clientKey, out var entityId))
                throw new InvalidOperationException($"Entity link references unknown clientKey '{clientKey}'.");

            db.MemoryEntities.Add(new MemoryEntity { MemoryId = memory.Id, EntityId = entityId });
        }

        var tokenIds = new List<Guid>();
        foreach (var tokenInput in input.Tokens ?? [])
        {
            var token = input.ReuseTokens
                ? await tokenService.FindOrCreateAsync(tokenInput, cancellationToken)
                : await tokenService.CreateAsync(tokenInput, cancellationToken);

            db.MemoryTokens.Add(new MemoryToken
            {
                Id = Guid.NewGuid(),
                MemoryId = memory.Id,
                TokenId = token.Id
            });
            tokenIds.Add(token.Id);
        }

        var relationshipIds = new List<Guid>();
        foreach (var relInput in input.Relationships ?? [])
        {
            if (!entityIds.TryGetValue(relInput.FromClientKey, out var fromId))
                throw new InvalidOperationException($"Relationship references unknown fromClientKey '{relInput.FromClientKey}'.");
            if (!entityIds.TryGetValue(relInput.ToClientKey, out var toId))
                throw new InvalidOperationException($"Relationship references unknown toClientKey '{relInput.ToClientKey}'.");

            var relationship = await relationshipService.CreateEntityAsync(fromId, toId, relInput.RelationType, memory.Id, relInput.Confidence, cancellationToken);
            relationshipIds.Add(relationship.Id);
        }

        return new BundleCoreResult(memory.Id, entityIds, tokenIds, relationshipIds);
    }

    private static void EnsureMutable(Memory memory)
    {
        if (memory.Status is MemoryStatus.Superseded or MemoryStatus.Invalid or MemoryStatus.Retracted)
            throw new InvalidOperationException($"Cannot update metadata on a {memory.Status} memory.");
    }

    private async Task AddRevisionAsync(
        Memory memory,
        MemoryRevisionType revisionType,
        string? note,
        DateTime? previousMemoryFrom,
        DateTime? newMemoryFrom,
        MemoryStatus? previousStatus,
        MemoryStatus? newStatus,
        Guid? successorMemoryId,
        string? successorRaw,
        CancellationToken cancellationToken)
    {
        db.MemoryRevisions.Add(new MemoryRevision
        {
            Id = Guid.NewGuid(),
            MemoryId = memory.Id,
            RevisionType = revisionType,
            Created = DateTime.UtcNow,
            Note = note,
            PreviousMemoryFrom = previousMemoryFrom,
            NewMemoryFrom = newMemoryFrom,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            SuccessorMemoryId = successorMemoryId,
            SuccessorRaw = successorRaw
        });
        await Task.CompletedTask;
    }

    private static MemoryDetailDto ToDetail(Memory memory, IReadOnlyList<EntityRelationship> relationships) =>
        new(
            memory.Ref ?? string.Empty,
            memory.Id,
            memory.Raw,
            memory.Created,
            memory.Updated,
            memory.MemoryFrom,
            memory.Status,
            memory.StatusNote,
            memory.SupersedesMemoryId,
            memory.SupersededByMemoryId,
            memory.Entities.Select(me => ModelMappers.ToSummary(me.Entity, 0)).ToList(),
            memory.Tokens.Select(mt => TokenValueHelper.ToSummary(mt.Token)).ToList(),
            relationships.Select(RelationshipService.ToSummary).ToList(),
            memory.Revisions.OrderByDescending(r => r.Created).Select(ToRevisionDto).ToList(),
            memory.SupersedesMemory is null ? null : ToSummary(memory.SupersedesMemory),
            memory.SupersededByMemory is null ? null : ToSummary(memory.SupersededByMemory));

    private static MemoryRevisionDto ToRevisionDto(MemoryRevision revision) =>
        new(
            revision.Id,
            revision.RevisionType,
            revision.Created,
            revision.Note,
            revision.PreviousMemoryFrom,
            revision.NewMemoryFrom,
            revision.PreviousStatus,
            revision.NewStatus,
            revision.SuccessorMemoryId,
            revision.SuccessorRaw);

    private static MemorySummaryDto ToSummary(Memory memory) => ModelMappers.ToSummary(memory);
}
