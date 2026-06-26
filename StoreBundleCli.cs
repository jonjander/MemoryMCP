using System.Text.Json;
using System.Text.Json.Serialization;
using MemoryMCP.Models;
using MemoryMCP.Services;
using Microsoft.Extensions.DependencyInjection;

public static class StoreBundleCli
{
    public static async Task<int> RunAsync(IServiceProvider services, string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            Console.Error.WriteLine($"Bundle file not found: {jsonPath}");
            return 1;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var input = JsonSerializer.Deserialize<StoreMemoryBundleInput>(json, JsonOptions())
            ?? throw new InvalidOperationException("Failed to deserialize bundle JSON.");

        using var scope = services.CreateScope();
        var memoryStore = scope.ServiceProvider.GetRequiredService<MemoryStoreService>();
        var result = await memoryStore.StoreBundleAsync(input);

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            memoryId = result.MemoryId,
            entityIds = result.EntityIds,
            tokenIds = result.TokenIds,
            relationshipIds = result.RelationshipIds
        }, JsonOptions()));

        return 0;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
