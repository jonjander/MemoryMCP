using MemoryMCP.Data;
using MemoryMCP.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Services;

public class EntityResolutionService(MemoryDbContext db)
{
    public async Task<Entity> ResolveOrCreateAsync(string type, string name, bool forceCreate = false, CancellationToken cancellationToken = default)
    {
        var normalizedType = type.Trim();
        var normalizedName = name.Trim();

        if (!forceCreate)
        {
            var exact = await db.Entities
                .Where(e => e.Status == EntityStatus.Active)
                .FirstOrDefaultAsync(e => e.Type == normalizedType && e.Name == normalizedName, cancellationToken);

            if (exact is not null)
                return exact;

            var caseInsensitive = await db.Entities
                .Where(e => e.Status == EntityStatus.Active)
                .FirstOrDefaultAsync(
                    e => e.Type.ToLower() == normalizedType.ToLower() && e.Name.ToLower() == normalizedName.ToLower(),
                    cancellationToken);

            if (caseInsensitive is not null)
                return caseInsensitive;
        }

        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            Type = normalizedType,
            Name = normalizedName,
            Status = EntityStatus.Active
        };
        db.Entities.Add(entity);
        return entity;
    }

    public async Task<EntitySummaryDto> CreateEntityAsync(string type, string name, CancellationToken cancellationToken = default)
    {
        var entity = await ResolveOrCreateAsync(type, name, forceCreate: false, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await ToSummaryAsync(entity.Id, cancellationToken);
    }

    public async Task<EntitySummaryDto> UpdateEntityAsync(
        Guid id,
        string? name = null,
        string? type = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(type))
            throw new InvalidOperationException("At least one of name or type must be provided.");

        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Entity not found.");

        EnsureActive(entity);

        var previousName = entity.Name;
        var previousType = entity.Type;

        if (!string.IsNullOrWhiteSpace(name))
            entity.Name = name.Trim();

        if (!string.IsNullOrWhiteSpace(type))
            entity.Type = type.Trim();

        await EnsureUniqueActiveEntityAsync(entity.Type, entity.Name, entity.Id, cancellationToken);

        entity.Updated = DateTime.UtcNow;

        if (!string.Equals(previousName, entity.Name, StringComparison.Ordinal))
        {
            await AddRevisionAsync(entity, EntityRevisionType.NameUpdated, note, previousName, entity.Name, previousType, entity.Type, entity.Status, entity.Status, null, cancellationToken);
        }

        if (!string.Equals(previousType, entity.Type, StringComparison.Ordinal))
        {
            await AddRevisionAsync(entity, EntityRevisionType.TypeUpdated, note, previousName, entity.Name, previousType, entity.Type, entity.Status, entity.Status, null, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await ToSummaryAsync(entity.Id, cancellationToken);
    }

    public async Task<MergeEntitiesResultDto> MergeEntitiesAsync(
        Guid sourceEntityId,
        Guid targetEntityId,
        string? targetName = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (sourceEntityId == targetEntityId)
            throw new InvalidOperationException("Source and target entity must differ.");

        var source = await db.Entities.FirstOrDefaultAsync(e => e.Id == sourceEntityId, cancellationToken)
            ?? throw new InvalidOperationException("Source entity not found.");
        var target = await db.Entities.FirstOrDefaultAsync(e => e.Id == targetEntityId, cancellationToken)
            ?? throw new InvalidOperationException("Target entity not found.");

        EnsureActive(source);
        EnsureActive(target);

        var memoryLinks = await db.MemoryEntities.Where(me => me.EntityId == sourceEntityId).ToListAsync(cancellationToken);
        var memoriesMoved = 0;
        foreach (var link in memoryLinks)
        {
            var exists = await db.MemoryEntities.AnyAsync(me => me.MemoryId == link.MemoryId && me.EntityId == targetEntityId, cancellationToken);
            if (exists)
            {
                db.MemoryEntities.Remove(link);
            }
            else
            {
                db.MemoryEntities.Remove(link);
                db.MemoryEntities.Add(new MemoryEntity
                {
                    MemoryId = link.MemoryId,
                    EntityId = targetEntityId
                });
                memoriesMoved++;
            }
        }

        var outgoing = await db.EntityRelationships.Where(r => r.FromEntityId == sourceEntityId).ToListAsync(cancellationToken);
        foreach (var rel in outgoing)
        {
            if (rel.ToEntityId == targetEntityId)
                db.EntityRelationships.Remove(rel);
            else
                rel.FromEntityId = targetEntityId;
        }

        var incoming = await db.EntityRelationships.Where(r => r.ToEntityId == sourceEntityId).ToListAsync(cancellationToken);
        foreach (var rel in incoming)
        {
            if (rel.FromEntityId == targetEntityId)
                db.EntityRelationships.Remove(rel);
            else
                rel.ToEntityId = targetEntityId;
        }

        var relationshipsMoved = outgoing.Count + incoming.Count;

        source.Status = EntityStatus.Merged;
        source.MergedIntoEntityId = targetEntityId;
        source.Name = $"{source.Name} [merged→{target.Name}]";
        source.Updated = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(targetName))
        {
            var previousTargetName = target.Name;
            target.Name = targetName.Trim();
            await EnsureUniqueActiveEntityAsync(target.Type, target.Name, target.Id, cancellationToken);
            await AddRevisionAsync(target, EntityRevisionType.NameUpdated, note, previousTargetName, target.Name, target.Type, target.Type, EntityStatus.Active, EntityStatus.Active, sourceEntityId, cancellationToken);
        }

        target.Updated = DateTime.UtcNow;

        await AddRevisionAsync(source, EntityRevisionType.Merged, note, previousName: null, newName: source.Name, previousType: null, newType: null, EntityStatus.Active, EntityStatus.Merged, targetEntityId, cancellationToken);
        await AddRevisionAsync(target, EntityRevisionType.Merged, note, target.Name, target.Name, target.Type, target.Type, EntityStatus.Active, EntityStatus.Active, sourceEntityId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new MergeEntitiesResultDto(
            sourceEntityId,
            targetEntityId,
            await ToSummaryAsync(sourceEntityId, cancellationToken),
            await ToSummaryAsync(targetEntityId, cancellationToken),
            memoriesMoved,
            relationshipsMoved);
    }

    public async Task<EntitySummaryDto> DeprecateEntityAsync(Guid id, string? note = null, CancellationToken cancellationToken = default)
    {
        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Entity not found.");

        EnsureActive(entity);

        var previousStatus = entity.Status;
        entity.Status = EntityStatus.Deprecated;
        entity.Updated = DateTime.UtcNow;

        await AddRevisionAsync(entity, EntityRevisionType.Deprecated, note, entity.Name, entity.Name, entity.Type, entity.Type, previousStatus, entity.Status, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await ToSummaryAsync(entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<EntitySummaryDto>> FindEntitiesAsync(
        string? name = null,
        string? type = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Entities.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(e => e.Status == EntityStatus.Active);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.Type.Contains(type));

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(e => e.Name.Contains(name));

        var entities = await query
            .Select(e => new
            {
                Entity = e,
                MemoryCount = e.Memories.Count
            })
            .OrderBy(x => x.Entity.Name)
            .Take(100)
            .ToListAsync(cancellationToken);

        return entities
            .Select(x => new EntitySummaryDto(x.Entity.Id, x.Entity.Type, x.Entity.Name, x.MemoryCount, x.Entity.Status, x.Entity.MergedIntoEntityId))
            .ToList();
    }

    public async Task<EntityDetailDto?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Entities
            .AsNoTracking()
            .Include(e => e.OutgoingRelationships).ThenInclude(r => r.ToEntity)
            .Include(e => e.IncomingRelationships).ThenInclude(r => r.FromEntity)
            .Include(e => e.Revisions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)
            return null;

        var memoryCount = await db.MemoryEntities.CountAsync(me => me.EntityId == id, cancellationToken);
        var recentMemories = await db.MemoryEntities
            .AsNoTracking()
            .Where(me => me.EntityId == id)
            .Select(me => me.Memory)
            .OrderByDescending(m => m.Created)
            .Take(10)
            .Select(m => new MemorySummaryDto(m.Id, m.Raw, m.Created, m.MemoryFrom, m.Status, m.SupersedesMemoryId, m.SupersededByMemoryId))
            .ToListAsync(cancellationToken);

        return new EntityDetailDto(
            entity.Id,
            entity.Type,
            entity.Name,
            memoryCount,
            entity.Status,
            entity.Updated,
            entity.MergedIntoEntityId,
            entity.OutgoingRelationships.Select(RelationshipService.ToSummary).ToList(),
            entity.IncomingRelationships.Select(RelationshipService.ToSummary).ToList(),
            recentMemories,
            entity.Revisions.OrderByDescending(r => r.Created).Select(ToRevisionDto).ToList());
    }

    public async Task<EntityHistoryDto?> GetEntityHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Entities.AsNoTracking().Include(e => e.Revisions).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
            return null;

        return new EntityHistoryDto(
            new EntitySummaryDto(entity.Id, entity.Type, entity.Name, entity.Memories.Count, entity.Status, entity.MergedIntoEntityId),
            entity.Revisions.OrderByDescending(r => r.Created).Select(ToRevisionDto).ToList());
    }

    private async Task EnsureUniqueActiveEntityAsync(string type, string name, Guid excludeId, CancellationToken cancellationToken)
    {
        var conflict = await db.Entities.AnyAsync(
            e => e.Id != excludeId && e.Status == EntityStatus.Active && e.Type == type && e.Name == name,
            cancellationToken);

        if (conflict)
            throw new InvalidOperationException($"An active entity with type '{type}' and name '{name}' already exists.");
    }

    private static void EnsureActive(Entity entity)
    {
        if (entity.Status != EntityStatus.Active)
            throw new InvalidOperationException($"Entity is {entity.Status} and cannot be modified.");
    }

    private async Task AddRevisionAsync(
        Entity entity,
        EntityRevisionType revisionType,
        string? note,
        string? previousName,
        string? newName,
        string? previousType,
        string? newType,
        EntityStatus? previousStatus,
        EntityStatus? newStatus,
        Guid? relatedEntityId,
        CancellationToken cancellationToken)
    {
        db.EntityRevisions.Add(new EntityRevision
        {
            Id = Guid.NewGuid(),
            EntityId = entity.Id,
            RevisionType = revisionType,
            Created = DateTime.UtcNow,
            Note = note,
            PreviousName = previousName,
            NewName = newName,
            PreviousType = previousType,
            NewType = newType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            RelatedEntityId = relatedEntityId
        });
        await Task.CompletedTask;
    }

    private async Task<EntitySummaryDto> ToSummaryAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Entities.AsNoTracking().FirstAsync(e => e.Id == id, cancellationToken);
        var memoryCount = await db.MemoryEntities.CountAsync(me => me.EntityId == id, cancellationToken);
        return new EntitySummaryDto(entity.Id, entity.Type, entity.Name, memoryCount, entity.Status, entity.MergedIntoEntityId);
    }

    private static EntityRevisionDto ToRevisionDto(EntityRevision revision) =>
        new(
            revision.Id,
            revision.RevisionType,
            revision.Created,
            revision.Note,
            revision.PreviousName,
            revision.NewName,
            revision.PreviousType,
            revision.NewType,
            revision.PreviousStatus,
            revision.NewStatus,
            revision.RelatedEntityId);
}
