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
        [Description("The original observation text, stored exactly as received. For large text (>~2k chars), write a file and pass rawPath instead.")] string? raw = null,
        [Description("Optional date when the observation occurred.")] DateTime? memoryFrom = null,
        [Description("JSON array of entities: [{\"clientKey\":\"boff\",\"type\":\"Wine\",\"name\":\"böff\"},{\"clientKey\":\"foo\",\"type\":\"Wine\",\"name\":\"foo\"}]")] string? entitiesJson = null,
        [Description(TokenPropertyGuidance.BundleTokensJson)] string? tokensJson = null,
        [Description("JSON array of entity clientKeys to link: [\"boff\",\"foo\"]")] string? entityLinksJson = null,
        [Description("JSON array of relationships: [{\"fromClientKey\":\"asa\",\"toClientKey\":\"sandra\",\"relationType\":\"SameAgeAs\",\"confidence\":0.9}]. " + TokenPropertyGuidance.RelationshipsVsTokensGuidance)] string? relationshipsJson = null,
        [Description(TokenPropertyGuidance.ReuseTokensGuidance)] bool reuseTokens = true,
        [Description("Absolute or relative path to a UTF-8 text file containing raw observation text. Overrides raw when set. Max 2 MB.")] string? rawPath = null,
        [Description("Absolute or relative path to a JSON file with the full bundle shape (same fields as store_memory_bundle). Overrides inline parameters when set. Max 2 MB.")] string? bundlePath = null,
        CancellationToken cancellationToken = default)
    {
        StoreMemoryBundleInput input;
        if (!string.IsNullOrWhiteSpace(bundlePath))
        {
            input = await PayloadPathReader.ReadJsonFileAsync<StoreMemoryBundleInput>(bundlePath, JsonOptions(), cancellationToken);
        }
        else
        {
            var resolvedRaw = await PayloadInputResolver.ResolveRawAsync(raw, rawPath, cancellationToken);
            input = new StoreMemoryBundleInput(
                resolvedRaw,
                memoryFrom,
                DeserializeList<BundleEntityInput>(entitiesJson),
                DeserializeList<BundleTokenInput>(tokensJson),
                DeserializeList<string>(entityLinksJson),
                DeserializeList<BundleRelationshipInput>(relationshipsJson),
                reuseTokens);
        }

        var result = await memoryStore.StoreBundleAsync(input, cancellationToken);
        var steps = JsonResult.MergeNextSteps(
            AgentGuidance.AfterStoreBundleSteps,
            AgentGuidance.StepsForExactRawDuplicate(result.ExactRawDuplicateWarning));
        return JsonResult.OkWithNextSteps(result, steps);
    }

    [McpServerTool, Description(
        "Store multiple memory bundles in one atomic transaction (max 100). " +
        "Each item has the same shape as store_memory_bundle: raw, memoryFrom, entities, tokens, entityLinks, relationships, reuseTokens. " +
        "Use instead of many separate store_memory_bundle calls. All bundles commit together or none do. " +
        "For large payloads, pass bundlesPath with a JSON array file instead of inlining bundlesJson. " +
        TokenPropertyGuidance.StoreBundleDescription)]
    public async Task<string> StoreMemoryBundles(
        [Description("JSON array of bundle objects. Each object: {\"raw\":\"...\",\"entities\":[{\"clientKey\":\"x\",\"type\":\"Person\",\"name\":\"Ann\"}],\"tokens\":[...],\"entityLinks\":[\"x\"],\"relationships\":[],\"reuseTokens\":true}. Max 100 items. For large payloads use bundlesPath.")] string? bundlesJson = null,
        [Description("Absolute or relative path to a UTF-8 JSON file containing the bundles array. Overrides bundlesJson when set. Max 2 MB.")] string? bundlesPath = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StoreMemoryBundleInput> bundles;
        if (!string.IsNullOrWhiteSpace(bundlesPath))
        {
            bundles = await PayloadPathReader.ReadJsonFileAsync<List<StoreMemoryBundleInput>>(bundlesPath, JsonOptions(), cancellationToken)
                ?? throw new InvalidOperationException("bundlesPath must contain a JSON array.");
        }
        else if (string.IsNullOrWhiteSpace(bundlesJson))
        {
            throw new InvalidOperationException("Either bundlesJson or bundlesPath is required.");
        }
        else
        {
            bundles = McpJson.DeserializeList<StoreMemoryBundleInput>(bundlesJson, "bundlesJson");
        }

        var result = await memoryStore.StoreBundlesAsync(bundles, cancellationToken);
        var steps = JsonResult.MergeNextSteps(
            AgentGuidance.AfterStoreBundlesSteps,
            AgentGuidance.StepsForBatchExactRawDuplicates(result));
        return JsonResult.OkWithNextSteps(result, steps);
    }

    private static IReadOnlyList<T>? DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions()) ?? [];
    }

    private static JsonSerializerOptions JsonOptions() => McpJson.Options;
}
