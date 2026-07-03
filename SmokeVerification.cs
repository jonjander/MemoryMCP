using MemoryMCP;
using MemoryMCP.Data;
using MemoryMCP.Models;
using MemoryMCP.Services;
using Microsoft.EntityFrameworkCore;
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
            Raw: "[smoke] Maja is 15 years old today. It is 2026.",
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
            Raw: "[smoke] Maja went to Stockholm.",
            Entities: [new BundleEntityInput("maja", "Person", "Maja")],
            EntityLinks: ["maja"]));

        if (bundle.EntityIds["maja"] != duplicateBundle.EntityIds["maja"])
            throw new InvalidOperationException("Entity deduplication failed for Maja.");

        var memory = await memoryStore.GetMemoryAsync(bundle.MemoryId);
        if (memory is null || memory.Raw != "[smoke] Maja is 15 years old today. It is 2026.")
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
                Raw: "[smoke] Batch wine A from 1990.",
                Entities: [new BundleEntityInput("wineA", "Wine", "Batch A")],
                Tokens: [new BundleTokenInput("Year", PropertyType.Int, IntValue: 1990)],
                EntityLinks: ["wineA"]),
            new StoreMemoryBundleInput(
                Raw: "[smoke] Batch wine B from 2001.",
                Entities: [new BundleEntityInput("wineB", "Wine", "Batch B")],
                Tokens: [new BundleTokenInput("Year", PropertyType.Int, IntValue: 2001)],
                EntityLinks: ["wineB"])
        ]);
        if (batch.Count != 2)
            throw new InvalidOperationException("Batch bundle store failed.");

        var batchToken = await tokenService.CreateAsync("Color", PropertyType.String, stringValue: "Red");
        var linkBatch = await memoryStore.LinkMemoryTokensAsync([
            new MemoryTokenLinkInput(batch.Results[0].Result.MemoryRef, batchToken.Ref),
            new MemoryTokenLinkInput(batch.Results[1].Result.MemoryRef, batchToken.Ref)
        ]);
        if (linkBatch.Linked != 2)
            throw new InvalidOperationException("Batch token link failed.");

        var createLinkBatch = await memoryStore.CreateAndLinkTokensAsync([
            new CreateAndLinkTokenInput(batch.Results[0].Result.MemoryRef, "Likes", PropertyType.String, StringValue: "cheese"),
            new CreateAndLinkTokenInput(batch.Results[1].Result.MemoryRef, "Likes", PropertyType.String, StringValue: "cheese")
        ]);
        if (createLinkBatch.Count != 2)
            throw new InvalidOperationException("Batch create-and-link tokens failed.");

        if (string.IsNullOrEmpty(bundle.MemoryRef) || bundle.MemoryRef.Length != RefIdGenerator.CharLength)
            throw new InvalidOperationException("Memory Ref id missing or wrong length.");

        if (!bundle.EntityRefs.TryGetValue("maja", out var majaRef) || string.IsNullOrEmpty(majaRef))
            throw new InvalidOperationException("Entity Ref id missing in bundle result.");

        var byRef = await searchService.SearchMemoriesByEntityAsync(entityId: majaRef);
        if (byRef.Count < 2)
            throw new InvalidOperationException("Search by entity Ref failed.");

        var duplicateRaw = "[smoke] Exact duplicate raw warning test.";
        var firstDup = await memoryStore.StoreBundleAsync(new StoreMemoryBundleInput(
            Raw: duplicateRaw,
            Entities: [new BundleEntityInput("x", "Person", "DupTestA")],
            EntityLinks: ["x"]));
        if (firstDup.ExactRawDuplicateWarning is not null)
            throw new InvalidOperationException("First store should not warn about duplicate raw.");

        var secondDup = await memoryStore.StoreBundleAsync(new StoreMemoryBundleInput(
            Raw: duplicateRaw,
            Entities: [new BundleEntityInput("y", "Person", "DupTestB")],
            EntityLinks: ["y"]));
        if (secondDup.ExactRawDuplicateWarning is null || secondDup.ExactRawDuplicateWarning.ExistingCount != 1)
            throw new InvalidOperationException("Second store with same raw should warn about one existing memory.");

        var untokenized = await memoryStore.ListMemoriesAsync(maxTokenCount: 0, take: 200);
        if (untokenized.TotalMatching < 1 || untokenized.Items.Any(i => i.TokenCount != 0))
            throw new InvalidOperationException("List memories maxTokenCount=0 failed.");

        var sparse = await memoryStore.ListMemoriesAsync(maxTokenCount: 2, sort: MemoryListSort.TokenCountAsc, take: 50);
        if (sparse.Items.Count < 1 || sparse.Items.Any(i => i.TokenCount > 2))
            throw new InvalidOperationException("List memories maxTokenCount=2 failed.");
        if (sparse.Items[0].TokenCount > sparse.Items[^1].TokenCount)
            throw new InvalidOperationException("List memories TokenCountAsc sort failed.");

        var db = scope.ServiceProvider.GetRequiredService<MemoryDbContext>();
        if (db.Database.IsSqlServer())
            await RunSqliteImportSmokeAsync(scope.ServiceProvider, entityService, searchService);

        await TestDataCleanup.RunAsync(services);

        Console.Error.WriteLine("Smoke verification passed (test data cleaned up).");
        return 0;
    }

    private static async Task RunSqliteImportSmokeAsync(
        IServiceProvider scopedServices,
        EntityResolutionService entityService,
        SearchService searchService)
    {
        var importService = scopedServices.GetRequiredService<SqliteImportService>();
        var tempPath = await CreateImportSourceSqliteAsync();

        try
        {
            await entityService.CreateEntityAsync("Person", "Alva");

            var preview = await importService.PreviewAsync(new SqliteImportOptions([tempPath]));
            if (preview.Totals.EntitiesReused < 1)
                throw new InvalidOperationException("SQLite import preview did not detect reusable entity.");

            if (preview.Totals.MemoriesImported != 2)
                throw new InvalidOperationException("SQLite import preview expected 2 memories to import.");

            var firstImport = await importService.ImportAsync(new SqliteImportOptions([tempPath]));
            if (firstImport.Totals.MemoriesImported != 2)
                throw new InvalidOperationException("SQLite import did not import expected memories.");

            if (firstImport.Totals.TokensReused < 1)
                throw new InvalidOperationException("SQLite import did not reuse shared Likes=pasta token.");

            var byToken = await searchService.SearchMemoriesByTokenAsync("Likes", stringValue: "pasta");
            if (byToken.Count < 2)
                throw new InvalidOperationException("Imported memories not found by shared token.");

            var secondImport = await importService.ImportAsync(new SqliteImportOptions([tempPath]));
            if (secondImport.Totals.MemoriesSkippedDuplicateRaw != 2)
                throw new InvalidOperationException("Second SQLite import did not skip duplicate Raw memories.");

            if (secondImport.Totals.MemoriesImported != 0)
                throw new InvalidOperationException("Second SQLite import should not add new memories.");
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static async Task<string> CreateImportSourceSqliteAsync()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"memorymcp-import-smoke-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<SqliteMemoryDbContext>()
            .UseSqlite($"Data Source={tempPath}")
            .Options;

        await using var sqliteDb = new SqliteMemoryDbContext(options);
        await sqliteDb.Database.MigrateAsync();

        var refResolver = new RefIdResolver(sqliteDb);
        var store = new MemoryStoreService(sqliteDb, refResolver);

        await store.StoreBundleAsync(new StoreMemoryBundleInput(
            Raw: "[import-smoke] Alva likes pasta.",
            Entities: [new BundleEntityInput("alva", "Person", "Alva")],
            Tokens: [new BundleTokenInput("Likes", PropertyType.String, StringValue: "pasta")],
            EntityLinks: ["alva"]));

        await store.StoreBundleAsync(new StoreMemoryBundleInput(
            Raw: "[import-smoke] Leo likes pasta.",
            Entities: [new BundleEntityInput("leo", "Person", "Leo")],
            Tokens: [new BundleTokenInput("Likes", PropertyType.String, StringValue: "pasta")],
            EntityLinks: ["leo"],
            ReuseTokens: true));

        return tempPath;
    }
}
