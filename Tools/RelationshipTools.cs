using System.ComponentModel;
using MemoryMCP.Services;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class RelationshipTools(RelationshipService relationshipService)
{
    [McpServerTool, Description("Create a relationship between two entities, e.g. FriendOf, DamagedBy, WorksAt.")]
    public async Task<string> CreateRelationship(
        Guid fromEntityId,
        Guid toEntityId,
        string relationType,
        Guid? memoryId = null,
        float confidence = 1f,
        CancellationToken cancellationToken = default)
    {
        var result = await relationshipService.CreateAsync(fromEntityId, toEntityId, relationType, memoryId, confidence, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Find relationships by from/to entity and/or relation type.")]
    public async Task<string> FindRelationships(
        Guid? fromEntityId = null,
        Guid? toEntityId = null,
        string? relationType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await relationshipService.FindRelationshipsAsync(fromEntityId, toEntityId, relationType, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Get an entity knowledge graph with relationships and recent memories.")]
    public async Task<string> GetEntityGraph(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var result = await relationshipService.GetEntityGraphAsync(entityId, cancellationToken);
        return result is null ? JsonResult.Ok(new { error = "Entity not found." }) : JsonResult.Ok(result);
    }
}
