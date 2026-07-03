using System.ComponentModel;
using MemoryMCP.Models;
using MemoryMCP.Services;
using ModelContextProtocol.Server;

namespace MemoryMCP.Tools;

[McpServerToolType]
public class ImportTools(SqliteImportService importService)
{
    [McpServerTool, Description(
        "Preview importing one or more MemoryMCP SQLite .db files into the configured SQL Server database. " +
        "Dry-run only — no writes. Reports entities/tokens reused vs new, memories imported vs skipped (duplicate Raw), and relationships. " +
        "Requires SQL Server target (not --typ sqlite). Run before import_sqlite_database.")]
    public async Task<string> PreviewSqliteImport(
        [Description("JSON array of absolute paths to MemoryMCP SQLite files, e.g. [\"C:\\\\data\\\\jon.db\"].")] string sourcePathsJson,
        [Description("Match existing entities by (Type, Name). Default true.")] bool reuseEntities = true,
        [Description("Match existing active tokens by (Property, Type, SearchValue). Default true.")] bool reuseTokens = true,
        [Description("Skip memories whose Raw text already exists in SQL Server. Default true.")] bool skipDuplicateRaw = true,
        CancellationToken cancellationToken = default)
    {
        var paths = McpJson.DeserializeList<string>(sourcePathsJson, "sourcePathsJson");
        var options = new SqliteImportOptions(paths, reuseEntities, reuseTokens, skipDuplicateRaw);
        var result = await importService.PreviewAsync(options, cancellationToken);
        return JsonResult.OkWithNextSteps(result, AgentGuidance.AfterSqliteImportPreviewSteps);
    }

    [McpServerTool, Description(
        "Import one or more MemoryMCP SQLite .db files into the configured SQL Server database. " +
        "Each file commits in its own transaction (all-or-nothing per file). " +
        "Entities and tokens are deduplicated deterministically — no AI. " +
        "Requires SQL Server target. Prefer preview_sqlite_import first.")]
    public async Task<string> ImportSqliteDatabase(
        [Description("JSON array of absolute paths to MemoryMCP SQLite files.")] string sourcePathsJson,
        [Description("Match existing entities by (Type, Name). Default true.")] bool reuseEntities = true,
        [Description("Match existing active tokens by (Property, Type, SearchValue). Default true.")] bool reuseTokens = true,
        [Description("Skip memories whose Raw text already exists in SQL Server. Default true.")] bool skipDuplicateRaw = true,
        CancellationToken cancellationToken = default)
    {
        var paths = McpJson.DeserializeList<string>(sourcePathsJson, "sourcePathsJson");
        var options = new SqliteImportOptions(paths, reuseEntities, reuseTokens, skipDuplicateRaw);
        var result = await importService.ImportAsync(options, cancellationToken);
        return JsonResult.OkWithNextSteps(result, AgentGuidance.AfterSqliteImportSteps);
    }
}
