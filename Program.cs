using MemoryMCP;
using MemoryMCP.Data;
using MemoryMCP.Services;
using MemoryMCP.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var (databaseType, hostArgs) = DatabaseBootstrap.ParseArgs(args);

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = hostArgs,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Configuration.AddEnvironmentVariables();

if (databaseType == DatabaseType.Sqlite)
{
    var sqliteConnectionString = DatabaseBootstrap.ResolveSqliteConnectionString();
    builder.Services.AddDbContext<SqliteMemoryDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));
    builder.Services.AddScoped<MemoryDbContext>(sp => sp.GetRequiredService<SqliteMemoryDbContext>());
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("MemoryMCP")
        ?? Environment.GetEnvironmentVariable("MEMORYMCP_CONNECTION_STRING");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'MemoryMCP' is not configured. Set it in appsettings.json next to the executable " +
            "or via the MEMORYMCP_CONNECTION_STRING / ConnectionStrings__MemoryMCP environment variable, " +
            "or use --typ sqlite for a local memory.db file.");
    }

    builder.Services.AddDbContext<MemoryDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddScoped<MemoryStoreService>();
builder.Services.AddScoped<EntityResolutionService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RelationshipService>();
builder.Services.AddScoped<SearchService>();

builder.Services
    .AddMcpServer(options => options.ServerInstructions = AgentGuidance.ServerInstructions)
    .WithStdioServerTransport()
    .WithTools<GuideTools>()
    .WithTools<MemoryTools>()
    .WithTools<EntityTools>()
    .WithTools<TokenTools>()
    .WithTools<RelationshipTools>()
    .WithTools<SearchTools>()
    .WithTools<BundleTools>()
    .WithResources<GuideResources>();

if (hostArgs.Contains("--list-tools"))
{
    return ListToolsCli.Run();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var db = scope.ServiceProvider.GetRequiredService<MemoryDbContext>();

    logger.LogInformation("Applying EF Core migrations ({DatabaseType})...", databaseType);
    await db.Database.MigrateAsync();
    logger.LogInformation("EF Core migrations applied.");

    await FullTextSearchInitializer.EnsureAsync(db);
}

if (hostArgs.Contains("--verify"))
{
    return await SmokeVerification.RunAsync(app.Services);
}

if (hostArgs.Contains("--cleanup-test-data"))
{
    return await TestDataCleanup.RunAsync(app.Services);
}

var storeBundleIndex = Array.IndexOf(hostArgs, "--store-bundle-json");
if (storeBundleIndex >= 0)
{
    if (storeBundleIndex + 1 >= hostArgs.Length)
    {
        Console.Error.WriteLine("Usage: --store-bundle-json <path-to-json>");
        return 1;
    }

    return await StoreBundleCli.RunAsync(app.Services, hostArgs[storeBundleIndex + 1]);
}

await app.RunAsync();
return 0;
