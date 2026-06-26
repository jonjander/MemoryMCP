using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Data;

public static class FullTextSearchInitializer
{
    private const string CatalogName = "MemoryMcpCatalog";
    private const string IndexName = "IX_Memories_Raw_FullText";

    public static async Task EnsureAsync(MemoryDbContext db, CancellationToken cancellationToken = default)
    {
        try
        {
            var catalogExists = await db.Database
                .SqlQueryRaw<int>("SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = {0}) THEN 1 ELSE 0 END AS [Value]", CatalogName)
                .FirstOrDefaultAsync(cancellationToken);

            if (catalogExists == 0)
            {
                await db.Database.ExecuteSqlRawAsync(
                    $"CREATE FULLTEXT CATALOG [{CatalogName}] AS DEFAULT;",
                    cancellationToken);
            }

            var indexExists = await db.Database
                .SqlQueryRaw<int>("SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.fulltext_indexes fi JOIN sys.tables t ON fi.object_id = t.object_id WHERE t.name = 'Memories') THEN 1 ELSE 0 END AS [Value]")
                .FirstOrDefaultAsync(cancellationToken);

            if (indexExists == 0)
            {
                await db.Database.ExecuteSqlRawAsync(
                    $"""
                    CREATE FULLTEXT INDEX ON [Memories]([Raw])
                    KEY INDEX [PK_Memories]
                    ON [{CatalogName}]
                    WITH CHANGE_TRACKING AUTO;
                    """,
                    cancellationToken);
            }
        }
        catch
        {
            // Full-text search requires SQL Server edition/feature support.
            // SearchService falls back to LIKE when FTS is unavailable.
        }
    }
}
