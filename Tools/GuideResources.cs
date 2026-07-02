using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerResourceType]
public class GuideResources(ServerStartupOptions startupOptions)
{
    [McpServerResource(UriTemplate = "memorymcp://guide/start", Name = "start", MimeType = "text/markdown")]
    [Description("Onboarding README: Ref ids, when to save vs ask, when to retrieve, store without structure prompts.")]
    public string StartGuide() => AgentGuidance.BuildStartHere(startupOptions.WhoAmI);

    [McpServerResource(UriTemplate = "memorymcp://guide/refs", Name = "refs", MimeType = "text/markdown")]
    [Description("Ref vs Guid: prefer 8-char Ref in tool calls; Guid backward compatible.")]
    public static string RefsGuide() => AgentGuidance.RefIdsGuide;

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
