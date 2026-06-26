using System.ComponentModel;
using MemoryMCP.Services;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class EntityTools(EntityResolutionService entityService, MemoryStoreService memoryStore, RefIdResolver refResolver)
{
    [McpServerTool, Description("Create or resolve an entity by type and name. Reuses existing entities when an exact match exists. Response includes short Ref id.")]
    public async Task<string> CreateEntity(
        [Description("Entity type, e.g. Person, Company, Location, Item.")] string type,
        [Description("Entity display name.")] string name,
        CancellationToken cancellationToken = default)
    {
        var result = await entityService.CreateEntityAsync(type, name, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Update entity name and/or type. Useful for fixing misspellings or reclassification.")]
    public async Task<string> UpdateEntity(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        string? name = null,
        string? type = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var entityId = await refResolver.ResolveEntityIdAsync(id, cancellationToken);
        var result = await entityService.UpdateEntityAsync(entityId, name, type, note, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Merge a duplicate entity into a target entity. Moves memory links and relationships; the source is marked Merged.")]
    public async Task<string> MergeEntities(
        [Description(RefIdResolver.IdOrRefDescription)] string sourceEntityId,
        [Description(RefIdResolver.IdOrRefDescription)] string targetEntityId,
        [Description("Optional new display name for the target entity after merge, e.g. full legal name.")] string? targetName = null,
        [Description("Optional audit note stored in revision history.")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        var sourceId = await refResolver.ResolveEntityIdAsync(sourceEntityId, cancellationToken);
        var targetId = await refResolver.ResolveEntityIdAsync(targetEntityId, cancellationToken);
        var result = await entityService.MergeEntitiesAsync(sourceId, targetId, targetName, note, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Mark an entity as deprecated without deleting it.")]
    public async Task<string> DeprecateEntity(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var entityId = await refResolver.ResolveEntityIdAsync(id, cancellationToken);
        var result = await entityService.DeprecateEntityAsync(entityId, note, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Find entities by partial name and/or type. Active entities only by default.")]
    public async Task<string> FindEntities(
        [Description("Partial name filter.")] string? name = null,
        [Description("Partial type filter.")] string? type = null,
        [Description("Include merged and deprecated entities.")] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await entityService.FindEntitiesAsync(name, type, includeInactive, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Link a memory to an entity.")]
    public async Task<string> LinkMemoryEntity(
        [Description(RefIdResolver.IdOrRefDescription)] string memoryId,
        [Description(RefIdResolver.IdOrRefDescription)] string entityId,
        CancellationToken cancellationToken = default)
    {
        var resolvedMemoryId = await refResolver.ResolveMemoryIdAsync(memoryId, cancellationToken);
        var resolvedEntityId = await refResolver.ResolveEntityIdAsync(entityId, cancellationToken);
        await memoryStore.LinkMemoryEntityAsync(resolvedMemoryId, resolvedEntityId, cancellationToken);
        return JsonResult.Ok(new { memoryId = resolvedMemoryId, entityId = resolvedEntityId, linked = true });
    }

    [McpServerTool, Description("Remove the link between a memory and an entity.")]
    public async Task<string> UnlinkMemoryEntity(
        [Description(RefIdResolver.IdOrRefDescription)] string memoryId,
        [Description(RefIdResolver.IdOrRefDescription)] string entityId,
        CancellationToken cancellationToken = default)
    {
        var resolvedMemoryId = await refResolver.ResolveMemoryIdAsync(memoryId, cancellationToken);
        var resolvedEntityId = await refResolver.ResolveEntityIdAsync(entityId, cancellationToken);
        await memoryStore.UnlinkMemoryEntityAsync(resolvedMemoryId, resolvedEntityId, cancellationToken);
        return JsonResult.Ok(new { memoryId = resolvedMemoryId, entityId = resolvedEntityId, linked = false });
    }

    [McpServerTool, Description("Get an entity with memory count, relationships, revisions, and recent memories.")]
    public async Task<string> GetEntity(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        CancellationToken cancellationToken = default)
    {
        var entityId = await refResolver.ResolveEntityIdAsync(id, cancellationToken);
        var result = await entityService.GetEntityAsync(entityId, cancellationToken);
        return result is null ? JsonResult.Ok(new { error = "Entity not found." }) : JsonResult.Ok(result);
    }

    [McpServerTool, Description("Get revision audit log for an entity.")]
    public async Task<string> GetEntityHistory(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        CancellationToken cancellationToken = default)
    {
        var entityId = await refResolver.ResolveEntityIdAsync(id, cancellationToken);
        var result = await entityService.GetEntityHistoryAsync(entityId, cancellationToken);
        return result is null ? JsonResult.Ok(new { error = "Entity not found." }) : JsonResult.Ok(result);
    }
}
