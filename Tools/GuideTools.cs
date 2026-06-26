using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class GuideTools
{
    [McpServerTool(Name = "start_here"), Description(
        "READ THIS FIRST — onboarding README for MemoryMCP. " +
        "Ref ids: prefer 8-char Ref from responses in follow-up calls; Guid still works everywhere. " +
        "When to SAVE: explicit orders ('spara i minnet', 'kom ihåg det här') → store_memory_bundle immediately. " +
        "Relevant fact without save request → may ask once 'Vill du att jag ska lägga detta i minnet?'. " +
        "When to RETRIEVE: user asks to recall or prior knowledge helps → search_* without asking. " +
        "Once saving: infer entities and tokens — never ask about Person entity structure.")]
    public static string StartHere() => AgentGuidance.StartHere;

    [McpServerTool, Description(
        "Get MemoryMCP workflow and usage guidance. Call start_here first if you are new. " +
        "Topics: overview (default), start_here, refs, quickstart, store, retrieve, tokens, maintenance, examples. " +
        "Also available as MCP resources: memorymcp://guide/start, memorymcp://guide/refs, memorymcp://guide/workflow, memorymcp://guide/tokens, memorymcp://guide/examples.")]
    public static string GetMemorymcpGuide(
        [Description("Guide topic: overview, start_here, refs, quickstart, store, retrieve, tokens, maintenance, examples. Omit for overview.")] string? topic = null)
    {
        return AgentGuidance.GetGuide(topic);
    }
}
