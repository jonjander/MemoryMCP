using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerResourceType]
public class GuideResources
{
    [McpServerResource(UriTemplate = "memorymcp://guide/workflow", Name = "workflow", MimeType = "text/markdown")]
    [Description("Full store and retrieve workflows with tool order.")]
    public static string WorkflowGuide() => AgentGuidance.GetGuide("store") + "\n\n---\n\n" + AgentGuidance.GetGuide("retrieve");

    [McpServerResource(UriTemplate = "memorymcp://guide/tokens", Name = "tokens", MimeType = "text/markdown")]
    [Description("Abstract token properties, mesh search rules, and tokens vs relationships.")]
    public static string TokensGuide() => AgentGuidance.GetGuide("tokens");

    [McpServerResource(UriTemplate = "memorymcp://guide/examples", Name = "examples", MimeType = "text/markdown")]
    [Description("store_memory_bundle JSON examples.")]
    public static string ExamplesGuide() => AgentGuidance.GetGuide("examples");
}
