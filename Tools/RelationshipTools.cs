using System.ComponentModel;
using MemoryMCP.Services;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class RelationshipTools(RelationshipService relationshipService, RefIdResolver refResolver)
{
    [McpServerTool, Description("Create a relationship between two entities, e.g. FriendOf, DamagedBy, WorksAt.")]
    public async Task<string> CreateRelationship(
        [Description(RefIdResolver.IdOrRefDescription)] string fromEntityId,
        [Description(RefIdResolver.IdOrRefDescription)] string toEntityId,
        string relationType,
        [Description(RefIdResolver.IdOrRefDescription)] string? memoryId = null,
        float confidence = 1f,
        CancellationToken cancellationToken = default)
    {
        var fromId = await refResolver.ResolveEntityIdAsync(fromEntityId, cancellationToken);
        var toId = await refResolver.ResolveEntityIdAsync(toEntityId, cancellationToken);
        Guid? resolvedMemoryId = null;
        if (!string.IsNullOrWhiteSpace(memoryId))
            resolvedMemoryId = await refResolver.ResolveMemoryIdAsync(memoryId, cancellationToken);

        var result = await relationshipService.CreateAsync(fromId, toId, relationType, resolvedMemoryId, confidence, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Find relationships by from/to entity and/or relation type.")]
    public async Task<string> FindRelationships(
        [Description(RefIdResolver.IdOrRefDescription)] string? fromEntityId = null,
        [Description(RefIdResolver.IdOrRefDescription)] string? toEntityId = null,
        string? relationType = null,
        CancellationToken cancellationToken = default)
    {
        Guid? fromId = null;
        Guid? toId = null;
        if (!string.IsNullOrWhiteSpace(fromEntityId))
            fromId = await refResolver.ResolveEntityIdAsync(fromEntityId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(toEntityId))
            toId = await refResolver.ResolveEntityIdAsync(toEntityId, cancellationToken);

        var result = await relationshipService.FindRelationshipsAsync(fromId, toId, relationType, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Get an entity knowledge graph with relationships and recent memories.")]
    public async Task<string> GetEntityGraph(
        [Description(RefIdResolver.IdOrRefDescription)] string entityId,
        CancellationToken cancellationToken = default)
    {
        var resolvedId = await refResolver.ResolveEntityIdAsync(entityId, cancellationToken);
        var result = await relationshipService.GetEntityGraphAsync(resolvedId, cancellationToken);
        return result is null ? JsonResult.Ok(new { error = "Entity not found." }) : JsonResult.Ok(result);
    }
}
