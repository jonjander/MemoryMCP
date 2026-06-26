using System.Reflection;
using ModelContextProtocol.Server;

public static class ListToolsCli
{
    public static int Run()
    {
        var toolTypes = typeof(ListToolsCli).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .OrderBy(t => t.Name)
            .ToList();

        var tools = new List<string>();
        foreach (var type in toolTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null)
                .Select(m => m.Name)
                .OrderBy(n => n);

            foreach (var method in methods)
                tools.Add(method);
        }

        var output = new
        {
            assembly = typeof(ListToolsCli).Assembly.Location,
            builtAt = File.GetLastWriteTimeUtc(typeof(ListToolsCli).Assembly.Location),
            toolCount = tools.Count,
            tools
        };

        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(output, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
