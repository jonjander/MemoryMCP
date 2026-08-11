using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MemoryMCP.Data;

public class MemoryDbContextFactory : IDesignTimeDbContextFactory<MemoryDbContext>
{
    public MemoryDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("MemoryMCP")
            ?? Environment.GetEnvironmentVariable("MEMORYMCP_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=MemoryMCP;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<MemoryDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new MemoryDbContext(optionsBuilder.Options);
    }
}
