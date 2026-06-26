using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class GuideTools
{
    [McpServerTool, Description(
        "Get MemoryMCP workflow and usage guidance. Call this first when unsure which tools to use or in what order. " +
        "Topics: overview (default), quickstart, store, retrieve, tokens, maintenance, examples. " +
        "Also available as MCP resources: memorymcp://guide/workflow, memorymcp://guide/tokens, memorymcp://guide/examples.")]
    public static string GetMemorymcpGuide(
        [Description("Guide topic: overview, quickstart, store, retrieve, tokens, maintenance, examples. Omit for overview.")] string? topic = null)
    {
        return AgentGuidance.GetGuide(topic);
    }
}
