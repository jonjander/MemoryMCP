using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MemoryMCP.Models;
using MemoryMCP.Services;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class BundleTools(MemoryStoreService memoryStore)
{
    [McpServerTool, Description(TokenPropertyGuidance.StoreBundleDescription)]
    public async Task<string> StoreMemoryBundle(
        [Description("The original observation text, stored exactly as received.")] string raw,
        [Description("Optional date when the observation occurred.")] DateTime? memoryFrom = null,
        [Description("JSON array of entities: [{\"clientKey\":\"boff\",\"type\":\"Wine\",\"name\":\"böff\"},{\"clientKey\":\"foo\",\"type\":\"Wine\",\"name\":\"foo\"}]")] string? entitiesJson = null,
        [Description(TokenPropertyGuidance.BundleTokensJson)] string? tokensJson = null,
        [Description("JSON array of entity clientKeys to link: [\"boff\",\"foo\"]")] string? entityLinksJson = null,
        [Description("JSON array of relationships: [{\"fromClientKey\":\"asa\",\"toClientKey\":\"sandra\",\"relationType\":\"SameAgeAs\",\"confidence\":0.9}]. " + TokenPropertyGuidance.RelationshipsVsTokensGuidance)] string? relationshipsJson = null,
        [Description(TokenPropertyGuidance.ReuseTokensGuidance)] bool reuseTokens = true,
        CancellationToken cancellationToken = default)
    {
        var input = new StoreMemoryBundleInput(
            raw,
            memoryFrom,
            DeserializeList<BundleEntityInput>(entitiesJson),
            DeserializeList<BundleTokenInput>(tokensJson),
            DeserializeList<string>(entityLinksJson),
            DeserializeList<BundleRelationshipInput>(relationshipsJson),
            reuseTokens);

        var result = await memoryStore.StoreBundleAsync(input, cancellationToken);
        return JsonResult.OkWithNextSteps(result, AgentGuidance.AfterStoreBundleSteps);
    }

    [McpServerTool, Description(
        "Store multiple memory bundles in one atomic transaction (max 100). " +
        "Each item has the same shape as store_memory_bundle: raw, memoryFrom, entities, tokens, entityLinks, relationships, reuseTokens. " +
        "Use instead of many separate store_memory_bundle calls. All bundles commit together or none do. " +
        TokenPropertyGuidance.StoreBundleDescription)]
    public async Task<string> StoreMemoryBundles(
        [Description("JSON array of bundle objects. Each object: {\"raw\":\"...\",\"entities\":[{\"clientKey\":\"x\",\"type\":\"Person\",\"name\":\"Ann\"}],\"tokens\":[...],\"entityLinks\":[\"x\"],\"relationships\":[],\"reuseTokens\":true}. Max 100 items.")] string bundlesJson,
        CancellationToken cancellationToken = default)
    {
        var bundles = McpJson.DeserializeList<StoreMemoryBundleInput>(bundlesJson, "bundlesJson");
        var result = await memoryStore.StoreBundlesAsync(bundles, cancellationToken);
        return JsonResult.OkWithNextSteps(result, AgentGuidance.AfterStoreBundlesSteps);
    }

    private static IReadOnlyList<T>? DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions()) ?? [];
    }

    private static JsonSerializerOptions JsonOptions() => McpJson.Options;
}
