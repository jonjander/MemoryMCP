using System.ComponentModel;
using MemoryMCP.Services;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class SearchTools(SearchService searchService)
{
    [McpServerTool, Description("Search memories by text using full-text search with LIKE fallback. Inactive memories are excluded by default.")]
    public async Task<string> SearchMemoriesByText(
        [Description("Search query matched against memory raw text.")] string query,
        [Description("Include superseded, invalid, and retracted memories.")] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchMemoriesByTextAsync(query, includeInactive, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Search memories linked to an entity by Ref id, Guid, or name.")]
    public async Task<string> SearchMemoriesByEntity(
        [Description(RefIdResolver.IdOrRefDescription + " Omit if using entityName.")] string? entityId = null,
        string? entityName = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchMemoriesByEntityAsync(entityId, entityName, includeInactive, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Search memories that have a token matching an abstract property and value.")]
    public async Task<string> SearchMemoriesByToken(
        [Description("Abstract property name, e.g. Year, Likes — shared across entities and memories.")] string property,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchMemoriesByTokenAsync(property, type, intValue, boolValue, stringValue, floatValue, dateTimeValue, includeInactive, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Find entities mentioned in memories that contain a matching abstract-property token.")]
    public async Task<string> SearchEntitiesByToken(
        [Description("Abstract property name, e.g. Year, Likes — shared across entities and memories.")] string property,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchEntitiesByTokenAsync(property, type, intValue, boolValue, stringValue, floatValue, dateTimeValue, cancellationToken);
        return JsonResult.Ok(result);
    }
}
