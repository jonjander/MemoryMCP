namespace MemoryMCP.Data;

public static class DatabaseBootstrap
{
    private const string TypeFlag = "--typ";
    private const string TypeEnvVar = "MEMORYMCP_TYP";
    private const string DefaultDbFileName = "memory.db";

    public static (DatabaseType Type, string[] RemainingArgs) ParseArgs(string[] args)
    {
        var type = ParseTypeFromEnv();
        var remaining = new List<string>(args.Length);

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], TypeFlag, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException($"Missing value after {TypeFlag}. Use 'sqlite' or 'sqlserver'.");

                type = ParseType(args[++i]);
                continue;
            }

            remaining.Add(args[i]);
        }

        return (type, remaining.ToArray());
    }

    public static string ResolveSqliteConnectionString()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, DefaultDbFileName);
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return $"Data Source={dbPath}";
    }

    private static DatabaseType ParseTypeFromEnv()
    {
        var envValue = Environment.GetEnvironmentVariable(TypeEnvVar);
        return string.IsNullOrWhiteSpace(envValue) ? DatabaseType.SqlServer : ParseType(envValue);
    }

    private static DatabaseType ParseType(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "sqlite" => DatabaseType.Sqlite,
            "sqlserver" or "sql-server" or "mssql" => DatabaseType.SqlServer,
            _ => throw new InvalidOperationException(
                $"Unknown database type '{value}'. Use 'sqlite' or 'sqlserver'.")
        };
}
