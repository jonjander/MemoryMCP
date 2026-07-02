namespace MemoryMCP.Data;

public static class DatabaseBootstrap
{
    private const string TypeFlag = "--typ";
    private const string DbNameFlag = "--dbName";
    private const string WhoAmIFlag = "--whoami";
    private const string TypeEnvVar = "MEMORYMCP_TYP";

    public static (DatabaseType Type, string[] RemainingArgs, ServerStartupOptions Options) ParseArgs(string[] args)
    {
        var type = ParseTypeFromEnv();
        var options = new ServerStartupOptions();
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

            if (string.Equals(args[i], DbNameFlag, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException($"Missing value after {DbNameFlag}.");

                options = options with { DbFileName = NormalizeDbFileName(args[++i]) };
                continue;
            }

            if (string.Equals(args[i], WhoAmIFlag, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException($"Missing value after {WhoAmIFlag}.");

                var whoAmI = args[++i].Trim();
                if (string.IsNullOrWhiteSpace(whoAmI))
                    throw new InvalidOperationException($"{WhoAmIFlag} requires a non-empty name (för- och efternamn).");

                options = options with { WhoAmI = whoAmI };
                continue;
            }

            remaining.Add(args[i]);
        }

        return (type, remaining.ToArray(), options);
    }

    public static string ResolveSqliteConnectionString(ServerStartupOptions options)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, options.DbFileName);
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return $"Data Source={dbPath}";
    }

    private static string NormalizeDbFileName(string value)
    {
        var fileName = value.Trim();
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException($"{DbNameFlag} requires a non-empty file name.");

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || fileName.Contains("..", StringComparison.Ordinal)
            || fileName.Contains('/', StringComparison.Ordinal)
            || fileName.Contains('\\', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{DbNameFlag} must be a simple file name (e.g. memory.db or jon-memory.db), not a path.");
        }

        return fileName;
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
