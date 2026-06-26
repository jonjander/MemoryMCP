using MemoryMCP.Data;
using MemoryMCP.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Services;

public class RelationshipService(MemoryDbContext db)
{
    public async Task<RelationshipSummaryDto> CreateAsync(
        Guid fromEntityId,
        Guid toEntityId,
        string relationType,
        Guid? memoryId = null,
        float confidence = 1f,
        CancellationToken cancellationToken = default)
    {
        var relationship = await CreateEntityAsync(fromEntityId, toEntityId, relationType, memoryId, confidence, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(relationship);
    }

    public async Task<EntityRelationship> CreateEntityAsync(
        Guid fromEntityId,
        Guid toEntityId,
        string relationType,
        Guid? memoryId = null,
        float confidence = 1f,
        CancellationToken cancellationToken = default)
    {
        var fromExists = await db.Entities.AnyAsync(e => e.Id == fromEntityId, cancellationToken);
        var toExists = await db.Entities.AnyAsync(e => e.Id == toEntityId, cancellationToken);
        if (!fromExists || !toExists)
            throw new InvalidOperationException("From or to entity not found.");

        var relationship = new EntityRelationship
        {
            Id = Guid.NewGuid(),
            FromEntityId = fromEntityId,
            ToEntityId = toEntityId,
            RelationType = relationType.Trim(),
            MemoryId = memoryId,
            Confidence = confidence,
            Created = DateTime.UtcNow
        };

        db.EntityRelationships.Add(relationship);
        return relationship;
    }

    public async Task<IReadOnlyList<RelationshipSummaryDto>> FindRelationshipsAsync(
        Guid? fromEntityId = null,
        Guid? toEntityId = null,
        string? relationType = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.EntityRelationships
            .AsNoTracking()
            .Include(r => r.FromEntity)
            .Include(r => r.ToEntity)
            .AsQueryable();

        if (fromEntityId.HasValue)
            query = query.Where(r => r.FromEntityId == fromEntityId.Value);

        if (toEntityId.HasValue)
            query = query.Where(r => r.ToEntityId == toEntityId.Value);

        if (!string.IsNullOrWhiteSpace(relationType))
            query = query.Where(r => r.RelationType == relationType);

        var relationships = await query.OrderByDescending(r => r.Created).Take(100).ToListAsync(cancellationToken);
        return relationships.Select(ToSummary).ToList();
    }

    public async Task<EntityGraphDto?> GetEntityGraphAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        var entityService = new EntityResolutionService(db);
        var entity = await entityService.GetEntityAsync(entityId, cancellationToken);
        if (entity is null)
            return null;

        return new EntityGraphDto(entity, entity.OutgoingRelationships, entity.IncomingRelationships, entity.RecentMemories);
    }

    public static RelationshipSummaryDto ToSummary(EntityRelationship relationship) =>
        new(
            relationship.Id,
            relationship.FromEntityId,
            relationship.FromEntity?.Name ?? string.Empty,
            relationship.ToEntityId,
            relationship.ToEntity?.Name ?? string.Empty,
            relationship.RelationType,
            relationship.Confidence,
            relationship.MemoryId);
}
