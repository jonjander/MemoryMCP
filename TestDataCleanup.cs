using MemoryMCP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class TestDataCleanup
{
    private static readonly string[] SmokeMemoryRaws =
    [
        "Maja is 15 years old today. It is 2026.",
        "Maja went to Stockholm."
    ];

    private static readonly string[] SmokeEntityNames =
    [
        "Maja",
        "David-sword",
        "Japan-sword"
    ];

    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoryDbContext>();

        var memories = await db.Memories
            .Where(m => SmokeMemoryRaws.Contains(m.Raw))
            .ToListAsync();

        db.Memories.RemoveRange(memories);

        var smokeEntityIds = await db.Entities
            .Where(e => SmokeEntityNames.Contains(e.Name))
            .Select(e => e.Id)
            .ToListAsync();

        var relationships = await db.EntityRelationships
            .Where(r => smokeEntityIds.Contains(r.FromEntityId) || smokeEntityIds.Contains(r.ToEntityId))
            .ToListAsync();
        db.EntityRelationships.RemoveRange(relationships);

        var entities = await db.Entities
            .Where(e => SmokeEntityNames.Contains(e.Name))
            .ToListAsync();
        db.Entities.RemoveRange(entities);

        var smokeTokens = await db.Tokens
            .Where(t =>
                (t.Property == "Age" && t.IntValue == 15) ||
                (t.Property == "Year" && t.IntValue == 2011) ||
                (t.Property == "Color" && t.StringValue == "Blue"))
            .ToListAsync();
        db.Tokens.RemoveRange(smokeTokens);

        await db.SaveChangesAsync();

        Console.Error.WriteLine($"Removed {memories.Count} smoke-test memories and related test data.");
        return 0;
    }
}
