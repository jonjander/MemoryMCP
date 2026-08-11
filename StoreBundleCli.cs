using System.Text.Json;
using System.Text.Json.Serialization;
using MemoryMCP.Models;
using MemoryMCP.Services;
using Microsoft.Extensions.DependencyInjection;

public static class StoreBundleCli
{
    public static async Task<int> RunAsync(IServiceProvider services, string jsonPath)
    {
        var input = await PayloadPathReader.ReadJsonFileAsync<StoreMemoryBundleInput>(jsonPath, JsonOptions());

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
