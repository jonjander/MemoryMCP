using MemoryMCP;
using MemoryMCP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class TestDataCleanup
{
    /// <summary>Exact raw texts from smoke verification — safe to delete from dev databases.</summary>
    private static readonly string[] SmokeMemoryRaws =
    [
        "Maja is 15 years old today. It is 2026.",
        "Maja went to Stockholm.",
        "[smoke] Maja is 15 years old today. It is 2026.",
        "[smoke] Maja went to Stockholm.",
        "Memory linked only to duplicate entity for merge smoke test.",
        "Batch wine A from 1990.",
        "Batch wine B from 2001.",
        "[smoke] Batch wine A from 1990.",
        "[smoke] Batch wine B from 2001."
    ];

    private static readonly string[] SmokeEntityNames =
    [
        "Maja",
        "Maja Duplicate",
        "David-sword",
        "Japan-sword",
        "Batch A",
        "Batch B"
    ];

    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoryDbContext>();

        var memories = await db.Memories
            .Where(m => SmokeMemoryRaws.Contains(m.Raw) || m.Raw.Contains("Maja went to Stockholm"))
            .ToListAsync();

        if (memories.Count > 0)
        {
            db.Memories.RemoveRange(memories);
            await db.SaveChangesAsync();
        }

        for (var round = 0; round < 32; round++)
        {
            var smokeEntityIds = await db.Entities
                .Where(e => SmokeEntityNames.Contains(e.Name))
                .Select(e => e.Id)
                .ToListAsync();
            if (smokeEntityIds.Count == 0)
                break;

            var mergeSources = await db.Entities
                .Where(e => e.MergedIntoEntityId != null && smokeEntityIds.Contains(e.MergedIntoEntityId.Value))
                .ToListAsync();
            if (mergeSources.Count > 0)
            {
                db.Entities.RemoveRange(mergeSources);
                await db.SaveChangesAsync();
                continue;
            }

            var relationships = await db.EntityRelationships
                .Where(r => smokeEntityIds.Contains(r.FromEntityId) || smokeEntityIds.Contains(r.ToEntityId))
                .ToListAsync();
            if (relationships.Count > 0)
            {
                db.EntityRelationships.RemoveRange(relationships);
                await db.SaveChangesAsync();
                continue;
            }

            var deletable = await db.Entities
                .Where(e => SmokeEntityNames.Contains(e.Name))
                .Where(e => !db.MemoryEntities.Any(me => me.EntityId == e.Id))
                .ToListAsync();
            if (deletable.Count == 0)
                break;

            db.Entities.RemoveRange(deletable);
            await db.SaveChangesAsync();
        }

        var orphanSmokeTokens = await db.Tokens
            .Where(t => !t.Memories.Any())
            .Where(t =>
                (t.Property == "Age" && t.IntValue == 15) ||
                (t.Property == "Year" && (t.IntValue == 2011 || t.IntValue == 1990 || t.IntValue == 2001)) ||
                (t.Property == "Color" && (t.StringValue == "Blue" || t.StringValue == "Red")) ||
                (t.Property == "Likes" && t.StringValue == "cheese"))
            .ToListAsync();
        if (orphanSmokeTokens.Count > 0)
        {
            db.Tokens.RemoveRange(orphanSmokeTokens);
            await db.SaveChangesAsync();
        }

        Console.Error.WriteLine($"Removed {memories.Count} smoke-test memories and related test data.");
        return 0;
    }
}
