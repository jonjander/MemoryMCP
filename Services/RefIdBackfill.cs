using MemoryMCP.Data;
using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Services;

public static class RefIdBackfill
{
    public static async Task EnsureAsync(MemoryDbContext db, CancellationToken cancellationToken = default)
    {
        await BackfillTableAsync(db, db.Entities.Where(e => e.Ref == null || e.Ref == ""), cancellationToken);
        await BackfillTableAsync(db, db.Memories.Where(m => m.Ref == null || m.Ref == ""), cancellationToken);
        await BackfillTableAsync(db, db.Tokens.Where(t => t.Ref == null || t.Ref == ""), cancellationToken);
        await EnsureUniqueIndexesAsync(db, cancellationToken);
    }

    private static async Task BackfillTableAsync<T>(MemoryDbContext db, IQueryable<T> query, CancellationToken cancellationToken)
        where T : class, IHasRef
    {
        const int batchSize = 100;

        while (true)
        {
            var batch = await query.OrderBy(e => e.Id).Take(batchSize).ToListAsync(cancellationToken);
            if (batch.Count == 0)
                break;

            foreach (var row in batch)
                row.Ref = RefIdGenerator.New();

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureUniqueIndexesAsync(MemoryDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Entities_Ref' AND object_id = OBJECT_ID(N'[Entities]'))
                CREATE UNIQUE INDEX [IX_Entities_Ref] ON [Entities] ([Ref]) WHERE [Ref] IS NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Memories_Ref' AND object_id = OBJECT_ID(N'[Memories]'))
                CREATE UNIQUE INDEX [IX_Memories_Ref] ON [Memories] ([Ref]) WHERE [Ref] IS NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tokens_Ref' AND object_id = OBJECT_ID(N'[Tokens]'))
                CREATE UNIQUE INDEX [IX_Tokens_Ref] ON [Tokens] ([Ref]) WHERE [Ref] IS NOT NULL;
            """,
            cancellationToken);
    }
}
