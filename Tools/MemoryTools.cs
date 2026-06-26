using System.ComponentModel;
using MemoryMCP.Services;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class MemoryTools(MemoryStoreService memoryStore, RefIdResolver refResolver)
{
    [McpServerTool, Description(
        "Store raw text only — missing entities and tokens. For new observations use store_memory_bundle instead; " +
        "do not ask the user about structure, extract entities and tokens from the text and store atomically.")]
    public async Task<string> CreateMemory(
        [Description("The original observation text, stored exactly as received.")] string raw,
        [Description("Optional date when the observation occurred.")] DateTime? memoryFrom = null,
        CancellationToken cancellationToken = default)
    {
        var result = await memoryStore.CreateMemoryAsync(raw, memoryFrom, cancellationToken);
        return JsonResult.OkWithNextSteps(result, AgentGuidance.AfterCreateMemorySteps);
    }

    [McpServerTool, Description("Get a memory with linked entities, tokens, relationships, and revision history.")]
    public async Task<string> GetMemory(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        CancellationToken cancellationToken = default)
    {
        var memoryId = await refResolver.ResolveMemoryIdAsync(id, cancellationToken);
        var result = await memoryStore.GetMemoryAsync(memoryId, cancellationToken);
        return result is null ? JsonResult.Ok(new { error = "Memory not found." }) : JsonResult.Ok(result);
    }

    [McpServerTool, Description("List memories with pagination. Inactive (superseded/invalid/retracted) memories are excluded by default.")]
    public async Task<string> ListMemories(
        [Description("Number of records to skip.")] int skip = 0,
        [Description("Maximum records to return (1-200).")] int take = 50,
        [Description("Only return memories created on or after this UTC timestamp.")] DateTime? createdSince = null,
        [Description("Include superseded, invalid, and retracted memories.")] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await memoryStore.ListMemoriesAsync(skip, take, createdSince, includeInactive, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Update when a memory observation occurred. Does not modify the raw text.")]
    public async Task<string> UpdateMemoryFrom(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        DateTime memoryFrom,
        [Description("Optional reason for the metadata update.")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        var memoryId = await refResolver.ResolveMemoryIdAsync(id, cancellationToken);
        var result = await memoryStore.UpdateMemoryFromAsync(memoryId, memoryFrom, note, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Mark a memory as invalid or retracted without deleting the original raw text.")]
    public async Task<string> InvalidateMemory(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        [Description("Invalid = factually wrong. Retracted = should no longer be used.")] MemoryStatus status,
        [Description("Why this memory is no longer trusted.")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        var memoryId = await refResolver.ResolveMemoryIdAsync(id, cancellationToken);
        var result = await memoryStore.InvalidateMemoryAsync(memoryId, status, note, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Create a corrected successor memory. The original raw text is preserved and marked Superseded.")]
    public async Task<string> ReviseMemory(
        [Description(RefIdResolver.IdOrRefDescription)] string originalId,
        [Description("Corrected observation text stored as a new memory.")] string correctedRaw,
        [Description("Optional corrected observation date.")] DateTime? memoryFrom = null,
        [Description("Why the memory was revised.")] string? note = null,
        [Description("Copy entity links from the original memory to the correction.")] bool copyEntityLinks = true,
        CancellationToken cancellationToken = default)
    {
        var resolvedOriginalId = await refResolver.ResolveMemoryIdAsync(originalId, cancellationToken);
        var result = await memoryStore.ReviseMemoryAsync(resolvedOriginalId, correctedRaw, memoryFrom, note, copyEntityLinks, cancellationToken);
        return JsonResult.Ok(result);
    }

    [McpServerTool, Description("Get revision audit log and the full correction chain for a memory.")]
    public async Task<string> GetMemoryHistory(
        [Description(RefIdResolver.IdOrRefDescription)] string id,
        CancellationToken cancellationToken = default)
    {
        var memoryId = await refResolver.ResolveMemoryIdAsync(id, cancellationToken);
        var result = await memoryStore.GetMemoryHistoryAsync(memoryId, cancellationToken);
        return result is null ? JsonResult.Ok(new { error = "Memory not found." }) : JsonResult.Ok(result);
    }
}
