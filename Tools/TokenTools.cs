using System.ComponentModel;
using System.Text.Json;
using MemoryMCP.Models;
using MemoryMCP.Services;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class TokenTools(TokenService tokenService, MemoryStoreService memoryStore, RefIdResolver refResolver)
{
    [McpServerTool, Description(TokenPropertyGuidance.CreateTokenDescription)]
    public async Task<string> CreateToken(
        [Description(TokenPropertyGuidance.PropertyParameter)] string property,
        [Description("Value type: Int, Bool, String, Float, DateTime.")] PropertyType type,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        [Description("Confidence between 0 and 1.")] float confidence = 1f,
        [Description("Extracted, Derived, or UserProvided.")] TokenSource source = TokenSource.Extracted,
        CancellationToken cancellationToken = default)
    {
        var result = await tokenService.CreateAsync(property, type, intValue, boolValue, stringValue, floatValue, dateTimeValue, confidence, source, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Update a token in place. Affects all linked memories. Recalculates SearchValue automatically.")]
    public async Task<string> UpdateToken(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        [Description(TokenPropertyGuidance.PropertyParameter)] string? property = null,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        float? confidence = null,
        TokenSource? source = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var tokenId = await refResolver.ResolveTokenIdAsync(id, cancellationToken);
        var result = await tokenService.UpdateTokenAsync(tokenId, property, type, intValue, boolValue, stringValue, floatValue, dateTimeValue, confidence, source, note, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Create a corrected successor token and relink memories. Original preserved when all links move.")]
    public async Task<string> SupersedeToken(
        [Description(RefIdResolver.IdOrRefDescription)] string originalTokenId,
        [Description(TokenPropertyGuidance.PropertyParameter)] string? property = null,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        float? confidence = null,
        TokenSource? source = null,
        [Description("Relink only this memory. Omit to relink all linked memories.")] string? memoryId = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedTokenId = await refResolver.ResolveTokenIdAsync(originalTokenId, cancellationToken);
        Guid? resolvedMemoryId = null;
        if (!string.IsNullOrWhiteSpace(memoryId))
            resolvedMemoryId = await refResolver.ResolveMemoryIdAsync(memoryId, cancellationToken);

        var result = await tokenService.SupersedeTokenAsync(resolvedTokenId, property, type, intValue, boolValue, stringValue, floatValue, dateTimeValue, confidence, source, resolvedMemoryId, note, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Mark a token as deprecated without deleting it.")]
    public async Task<string> DeprecateToken(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var tokenId = await refResolver.ResolveTokenIdAsync(id, cancellationToken);
        var result = await tokenService.DeprecateTokenAsync(tokenId, note, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Link a token to a memory.")]
    public async Task<string> LinkMemoryToken(
        [Description(RefIdResolver.IdOrRefDescription)] string memoryId,
        [Description(RefIdResolver.IdOrRefDescription)] string tokenId,
        CancellationToken cancellationToken = default)
    {
        var resolvedMemoryId = await refResolver.ResolveMemoryIdAsync(memoryId, cancellationToken);
        var resolvedTokenId = await refResolver.ResolveTokenIdAsync(tokenId, cancellationToken);
        await memoryStore.LinkMemoryTokenAsync(resolvedMemoryId, resolvedTokenId, cancellationToken);
        return JsonResult.Ok(new { memoryId = resolvedMemoryId, tokenId = resolvedTokenId, linked = true });
    }

    [McpServerTool, Description(
        "Link multiple existing tokens to memories in one transaction (max 500). " +
        "Use when tokens already exist and you need to attach them to several memories. " +
        "Skips links that already exist.")]
    public async Task<string> LinkMemoryTokens(
        [Description("JSON array: [{\"memoryId\":\"RefOrGuid\",\"tokenId\":\"RefOrGuid\"}, ...]")] string linksJson,
        CancellationToken cancellationToken = default)
    {
        var links = McpJson.DeserializeList<MemoryTokenLinkInput>(linksJson, "linksJson");
        var result = await memoryStore.LinkMemoryTokensAsync(links, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description(
        "Create multiple tokens and link each to a memory in one transaction (max 500). " +
        "Use when appending structured facts to existing memories without separate create_token + link_memory_token calls. " +
        TokenPropertyGuidance.CreateTokenDescription)]
    public async Task<string> CreateAndLinkTokens(
        [Description("JSON array. Each item: {\"memoryId\":\"RefOrGuid\",\"property\":\"Year\",\"type\":\"Int\",\"intValue\":1988,\"confidence\":0.95,\"source\":\"Extracted\",\"reuseToken\":true}")] string tokensJson,
        CancellationToken cancellationToken = default)
    {
        var items = McpJson.DeserializeList<CreateAndLinkTokenInput>(tokensJson, "tokensJson");
        var result = await memoryStore.CreateAndLinkTokensAsync(items, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Remove the link between a memory and a token.")]
    public async Task<string> UnlinkMemoryToken(
        [Description(RefIdResolver.IdOrRefDescription)] string memoryId,
        [Description(RefIdResolver.IdOrRefDescription)] string tokenId,
        CancellationToken cancellationToken = default)
    {
        var resolvedMemoryId = await refResolver.ResolveMemoryIdAsync(memoryId, cancellationToken);
        var resolvedTokenId = await refResolver.ResolveTokenIdAsync(tokenId, cancellationToken);
        await memoryStore.UnlinkMemoryTokenAsync(resolvedMemoryId, resolvedTokenId, cancellationToken);
        return JsonResult.Ok(new { memoryId = resolvedMemoryId, tokenId = resolvedTokenId, linked = false });
    }

    [McpServerTool, Description("Get a token with linked memory ids and revision history.")]
    public async Task<string> GetToken(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        CancellationToken cancellationToken = default)
    {
        var tokenId = await refResolver.ResolveTokenIdAsync(id, cancellationToken);
        var result = await tokenService.GetTokenAsync(tokenId, cancellationToken);
        return result is null ? JsonResult.Ok(new { error = "Token not found." }) : JsonResult.Ok(result);
    }

    [McpServerTool, Description("Get revision audit log and correction chain for a token.")]
    public async Task<string> GetTokenHistory(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        CancellationToken cancellationToken = default)
    {
        var tokenId = await refResolver.ResolveTokenIdAsync(id, cancellationToken);
        var result = await tokenService.GetTokenHistoryAsync(tokenId, cancellationToken);
        return result is null ? JsonResult.Ok(new { error = "Token not found." }) : JsonResult.Ok(result);
    }

    [McpServerTool, Description("Find tokens by abstract property name and value filters. Active tokens only by default.")]
    public async Task<string> FindTokens(
        [Description("Abstract property name, e.g. Year, Likes, Name — not entity-prefixed.")] string? property = null,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await tokenService.FindTokensAsync(property, type, intValue, boolValue, stringValue, floatValue, dateTimeValue, includeInactive, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description(TokenPropertyGuidance.ListPropertiesDescription)]
    public async Task<string> ListTokenProperties(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await tokenService.ListPropertiesAsync(includeInactive, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description(TokenPropertyGuidance.ListTokensByPropertyDescription)]
    public async Task<string> ListTokensByProperty(
        bool includeInactive = false,
        [Description("Optional: limit to one property name, e.g. Year.")] string? property = null,
        [Description("Max tokens returned per property group. Default 25.")] int maxTokensPerProperty = 25,
        [Description("Optional: only include tokens linked to at most this many memories (spot rare values).")] int? maxMemoryLinks = null,
        CancellationToken cancellationToken = default)
    {
        var result = await tokenService.ListTokensByPropertyAsync(
            includeInactive, property, maxTokensPerProperty, maxMemoryLinks, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description(TokenPropertyGuidance.RenamePropertyDescription)]
    public async Task<string> RenameTokenProperty(
        [Description("Current property name to rename, e.g. SofiaBirthYear.")] string fromProperty,
        [Description("Target abstract property name, e.g. Year.")] string toProperty,
        string? note = null,
        [Description("When true, returns affected token ids without applying changes.")] bool preview = false,
        CancellationToken cancellationToken = default)
    {
        var result = await tokenService.RenamePropertyAsync(fromProperty, toProperty, note, preview, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description(TokenPropertyGuidance.MergePropertiesDescription)]
    public async Task<string> MergeTokenProperties(
        [Description("JSON array of source property names: [\"SofiaBirthYear\",\"JonBirthYear\",\"BirthYear\"]")] string fromPropertiesJson,
        [Description("Canonical target property name, e.g. Year.")] string toProperty,
        string? note = null,
        bool preview = false,
        CancellationToken cancellationToken = default)
    {
        var fromProperties = JsonSerializer.Deserialize<List<string>>(fromPropertiesJson)
            ?? throw new InvalidOperationException("fromPropertiesJson must be a JSON array of strings.");

        var result = await tokenService.MergePropertiesAsync(fromProperties, toProperty, note, preview, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description(TokenPropertyGuidance.SplitTokenDescription)]
    public async Task<string> SplitTokenValue(
        [Description(RefIdResolver.IdOrRefDescription)] string tokenId,
        [Description("Abstract property for each split part, e.g. Likes.")] string targetProperty,
        [Description("Delimiter between values in the original string. Default comma.")] string delimiter = ",",
        string? note = null,
        bool preview = false,
        CancellationToken cancellationToken = default)
    {
        var resolvedTokenId = await refResolver.ResolveTokenIdAsync(tokenId, cancellationToken);
        var result = await tokenService.SplitTokenValueAsync(resolvedTokenId, targetProperty, delimiter, note, preview, cancellationToken);
        return JsonResult.Ok(result);
    }
}
