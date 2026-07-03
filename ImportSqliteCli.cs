using System.Text.Json;
using System.Text.Json.Serialization;
using MemoryMCP.Models;
using MemoryMCP.Services;
using Microsoft.Extensions.DependencyInjection;

public static class ImportSqliteCli
{
    private const string ImportFlag = "--import-sqlite";
    private const string PreviewFlag = "--preview";

    public static bool TryParse(string[] args, out string[] sourcePaths, out bool preview)
    {
        sourcePaths = [];
        preview = false;

        var index = Array.IndexOf(args, ImportFlag);
        if (index < 0)
            return false;

        var paths = new List<string>();
        for (var i = index + 1; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                if (string.Equals(args[i], PreviewFlag, StringComparison.OrdinalIgnoreCase))
                    preview = true;
                else
                    break;

                continue;
            }

            paths.Add(args[i]);
        }

        if (paths.Count == 0)
            throw new InvalidOperationException($"Usage: {ImportFlag} <path1.db> [path2.db ...] [{PreviewFlag}]");

        sourcePaths = paths.ToArray();
        return true;
    }

    public static async Task<int> RunAsync(IServiceProvider services, string[] sourcePaths, bool preview)
    {
        using var scope = services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<SqliteImportService>();
        var options = new SqliteImportOptions(sourcePaths);

        if (preview)
        {
            var result = await importService.PreviewAsync(options);
            Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions()));
            return 0;
        }

        var importResult = await importService.ImportAsync(options);
        Console.WriteLine(JsonSerializer.Serialize(importResult, JsonOptions()));
        return 0;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
