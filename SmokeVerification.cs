using MemoryMCP;
using MemoryMCP.Models;
using MemoryMCP.Services;
using Microsoft.Extensions.DependencyInjection;

public static class SmokeVerification
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var memoryStore = scope.ServiceProvider.GetRequiredService<MemoryStoreService>();
        var entityService = scope.ServiceProvider.GetRequiredService<EntityResolutionService>();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var relationshipService = scope.ServiceProvider.GetRequiredService<RelationshipService>();
        var searchService = scope.ServiceProvider.GetRequiredService<SearchService>();

        var bundle = await memoryStore.StoreBundleAsync(new StoreMemoryBundleInput(
            Raw: "Maja is 15 years old today. It is 2026.",
            MemoryFrom: new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc),
            Entities: [new BundleEntityInput("maja", "Person", "Maja")],
            Tokens:
            [
                new BundleTokenInput("Age", PropertyType.Int, IntValue: 15, Confidence: 0.95f, Source: TokenSource.Extracted),
                new BundleTokenInput("Year", PropertyType.Int, IntValue: 2011, Confidence: 0.75f, Source: TokenSource.Derived)
            ],
            EntityLinks: ["maja"],
            Relationships: []));

        var duplicateBundle = await memoryStore.StoreBundleAsync(new StoreMemoryBundleInput(
            Raw: "Maja went to Stockholm.",
            Entities: [new BundleEntityInput("maja", "Person", "Maja")],
            EntityLinks: ["maja"]));

        if (bundle.EntityIds["maja"] != duplicateBundle.EntityIds["maja"])
            throw new InvalidOperationException("Entity deduplication failed for Maja.");

        var memory = await memoryStore.GetMemoryAsync(bundle.MemoryId);
        if (memory is null || memory.Raw != "Maja is 15 years old today. It is 2026.")
            throw new InvalidOperationException("Memory retrieval or raw immutability failed.");

        var byEntity = await searchService.SearchMemoriesByEntityAsync(entityName: "Maja");
        if (byEntity.Count < 2)
            throw new InvalidOperationException("Search by entity failed.");

        var byToken = await searchService.SearchMemoriesByTokenAsync("Age", intValue: 15);
        if (byToken.Count < 1)
            throw new InvalidOperationException("Search by token failed.");

        var byText = await searchService.SearchMemoriesByTextAsync("2026");
        if (byText.Count < 1)
            throw new InvalidOperationException("Search by text failed.");

        var david = await entityService.CreateEntityAsync("Item", "David-sword");
        var japan = await entityService.CreateEntityAsync("Item", "Japan-sword");
        await relationshipService.CreateAsync(david.Id, japan.Id, "DamagedBy", bundle.MemoryId, 0.9f);
        var graph = await relationshipService.GetEntityGraphAsync(david.Id);
        if (graph is null || graph.OutgoingRelationships.Count < 1)
            throw new InvalidOperationException("Entity graph failed.");

        var duplicatePerson = await entityService.CreateEntityAsync("Person", "Maja Duplicate");
        await memoryStore.StoreBundleAsync(new StoreMemoryBundleInput(
            Raw: "Memory linked only to duplicate entity for merge smoke test.",
            Entities: [new BundleEntityInput("duponly", "Person", "Maja Duplicate")],
            EntityLinks: ["duponly"]));
        var mergeResult = await entityService.MergeEntitiesAsync(
            duplicatePerson.Id,
            bundle.EntityIds["maja"],
            targetName: "Maja",
            note: "Smoke test merge");
        if (mergeResult.MemoriesMoved < 1)
            throw new InvalidOperationException("Entity merge failed to move memory links.");
        var mergedSource = await entityService.GetEntityAsync(duplicatePerson.Id);
        if (mergedSource is null || mergedSource.Status != EntityStatus.Merged)
            throw new InvalidOperationException("Merged source entity status incorrect.");
        var mergeTarget = await entityService.GetEntityAsync(bundle.EntityIds["maja"]);
        if (mergeTarget is null || mergeTarget.MemoryCount < 1)
            throw new InvalidOperationException("Merge target entity missing memory links.");

        var token = await tokenService.CreateAsync("Color", PropertyType.String, stringValue: "Blue");
        await memoryStore.LinkMemoryTokenAsync(bundle.MemoryId, token.Id);

        var batch = await memoryStore.StoreBundlesAsync([
            new StoreMemoryBundleInput(
                Raw: "Batch wine A from 1990.",
                Entities: [new BundleEntityInput("wineA", "Wine", "Batch A")],
                Tokens: [new BundleTokenInput("Year", PropertyType.Int, IntValue: 1990)],
                EntityLinks: ["wineA"]),
            new StoreMemoryBundleInput(
                Raw: "Batch wine B from 2001.",
                Entities: [new BundleEntityInput("wineB", "Wine", "Batch B")],
                Tokens: [new BundleTokenInput("Year", PropertyType.Int, IntValue: 2001)],
                EntityLinks: ["wineB"])
        ]);
        if (batch.Count != 2)
            throw new InvalidOperationException("Batch bundle store failed.");

        var batchToken = await tokenService.CreateAsync("Color", PropertyType.String, stringValue: "Red");
        var linkBatch = await memoryStore.LinkMemoryTokensAsync([
            new MemoryTokenLinkInput(batch.Results[0].Result.MemoryId, batchToken.Id),
            new MemoryTokenLinkInput(batch.Results[1].Result.MemoryId, batchToken.Id)
        ]);
        if (linkBatch.Linked != 2)
            throw new InvalidOperationException("Batch token link failed.");

        var createLinkBatch = await memoryStore.CreateAndLinkTokensAsync([
            new CreateAndLinkTokenInput(batch.Results[0].Result.MemoryId, "Likes", PropertyType.String, StringValue: "cheese"),
            new CreateAndLinkTokenInput(batch.Results[1].Result.MemoryId, "Likes", PropertyType.String, StringValue: "cheese")
        ]);
        if (createLinkBatch.Count != 2)
            throw new InvalidOperationException("Batch create-and-link tokens failed.");

        await TestDataCleanup.RunAsync(services);

        Console.Error.WriteLine("Smoke verification passed (test data cleaned up).");
        return 0;
    }
}
