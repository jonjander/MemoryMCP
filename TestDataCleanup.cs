using MemoryMCP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class TestDataCleanup
{
    private static readonly string[] SmokeMemoryRaws =
    [
        "Maja is 15 years old today. It is 2026.",
        "Maja went to Stockholm.",
        "Memory linked only to duplicate entity for merge smoke test.",
        "Batch wine A from 1990.",
        "Batch wine B from 2001."
    ];

    private static readonly string[] SmokeEntityNamePrefixes =
    [
        "Maja",
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
            .Where(m => SmokeMemoryRaws.Contains(m.Raw))
            .ToListAsync();
        var memoryIds = memories.Select(m => m.Id).ToList();

        var entities = await db.Entities
            .Where(e => SmokeEntityNamePrefixes.Any(prefix => e.Name.StartsWith(prefix)))
            .ToListAsync();
        var entityIds = entities.Select(e => e.Id).ToList();

        var relationships = await db.EntityRelationships
            .Where(r => entityIds.Contains(r.FromEntityId) || entityIds.Contains(r.ToEntityId) || (r.MemoryId != null && memoryIds.Contains(r.MemoryId.Value)))
            .ToListAsync();
        db.EntityRelationships.RemoveRange(relationships);

        var entityRevisions = await db.EntityRevisions
            .Where(r => entityIds.Contains(r.EntityId))
            .ToListAsync();
        db.EntityRevisions.RemoveRange(entityRevisions);

        var memoryRevisions = await db.MemoryRevisions
            .Where(r => memoryIds.Contains(r.MemoryId))
            .ToListAsync();
        db.MemoryRevisions.RemoveRange(memoryRevisions);

        var memoryTokens = await db.MemoryTokens
            .Where(mt => memoryIds.Contains(mt.MemoryId))
            .ToListAsync();
        db.MemoryTokens.RemoveRange(memoryTokens);

        var memoryEntities = await db.MemoryEntities
            .Where(me => memoryIds.Contains(me.MemoryId) || entityIds.Contains(me.EntityId))
            .ToListAsync();
        db.MemoryEntities.RemoveRange(memoryEntities);

        foreach (var memory in memories)
        {
            memory.SupersedesMemoryId = null;
            memory.SupersededByMemoryId = null;
        }

        db.Memories.RemoveRange(memories);

        var smokeTokens = await db.Tokens
            .Where(t =>
                (t.Property == "Age" && t.IntValue == 15) ||
                (t.Property == "Year" && (t.IntValue == 2011 || t.IntValue == 1990 || t.IntValue == 2001)) ||
                (t.Property == "Color" && (t.StringValue == "Blue" || t.StringValue == "Red")) ||
                (t.Property == "Likes" && t.StringValue == "cheese"))
            .ToListAsync();
        var tokenIds = smokeTokens.Select(t => t.Id).ToList();

        var tokenRevisions = await db.TokenRevisions
            .Where(r => tokenIds.Contains(r.TokenId))
            .ToListAsync();
        db.TokenRevisions.RemoveRange(tokenRevisions);

        var remainingMemoryTokens = await db.MemoryTokens
            .Where(mt => tokenIds.Contains(mt.TokenId))
            .ToListAsync();
        db.MemoryTokens.RemoveRange(remainingMemoryTokens);

        foreach (var token in smokeTokens)
        {
            token.SupersedesTokenId = null;
            token.SupersededByTokenId = null;
        }

        db.Tokens.RemoveRange(smokeTokens);

        foreach (var entity in entities)
            entity.MergedIntoEntityId = null;

        db.Entities.RemoveRange(entities);

        await db.SaveChangesAsync();

        Console.Error.WriteLine($"Removed {memories.Count} smoke-test memories and related test data.");
        return 0;
    }
}
