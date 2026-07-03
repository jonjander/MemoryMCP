using MemoryMCP.Data;
using MemoryMCP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemoryMCP.Services;

public class SqliteImportService(
    MemoryDbContext db,
    EntityResolutionService entityResolution,
    TokenService tokenService,
    ILogger<SqliteImportService> logger)
{
    private sealed record SourceSnapshot(
        IReadOnlyList<Entity> Entities,
        IReadOnlyList<Token> Tokens,
        IReadOnlyList<Memory> Memories,
        IReadOnlyList<MemoryEntity> MemoryEntities,
        IReadOnlyList<MemoryToken> MemoryTokens,
        IReadOnlyList<EntityRelationship> Relationships);

    private sealed record FileCounters
    {
        public int EntitiesReused { get; set; }
        public int EntitiesNew { get; set; }
        public int MergedEntitiesInSource { get; set; }
        public int TokensReused { get; set; }
        public int TokensNew { get; set; }
        public int MemoriesImported { get; set; }
        public int MemoriesSkippedDuplicateRaw { get; set; }
        public int RelationshipsImported { get; set; }
        public int RelationshipsSkipped { get; set; }
        public List<string> Warnings { get; } = [];
    }

    public async Task<SqliteImportPreviewResult> PreviewAsync(
        SqliteImportOptions options,
        CancellationToken cancellationToken = default)
    {
        EnsureSqlServerTarget();
        var validatedPaths = ValidateSourcePaths(options.SourcePaths);
        var filePreviews = new List<SqliteImportFilePreview>(validatedPaths.Count);

        foreach (var path in validatedPaths)
        {
            await using var sourceDb = CreateReadOnlySourceContext(path);
            await ValidateSourceSchemaAsync(sourceDb, path, cancellationToken);
            var snapshot = await LoadSourceSnapshotAsync(sourceDb, cancellationToken);
            var counters = await AnalyzeFileAsync(snapshot, options, dryRun: true, cancellationToken);
            filePreviews.Add(ToFilePreview(path, counters));
        }

        return new SqliteImportPreviewResult(filePreviews, SumFilePreviews(filePreviews));
    }

    public async Task<SqliteImportResult> ImportAsync(
        SqliteImportOptions options,
        CancellationToken cancellationToken = default)
    {
        EnsureSqlServerTarget();
        var validatedPaths = ValidateSourcePaths(options.SourcePaths);
        var fileResults = new List<SqliteImportFileResult>(validatedPaths.Count);

        foreach (var path in validatedPaths)
        {
            logger.LogInformation("Importing SQLite database from {SourcePath}", path);

            await using var sourceDb = CreateReadOnlySourceContext(path);
            await ValidateSourceSchemaAsync(sourceDb, path, cancellationToken);
            var snapshot = await LoadSourceSnapshotAsync(sourceDb, cancellationToken);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var counters = await AnalyzeFileAsync(snapshot, options, dryRun: false, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                fileResults.Add(ToFileResult(path, counters));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        return new SqliteImportResult(fileResults, SumFileResults(fileResults));
    }

    private void EnsureSqlServerTarget()
    {
        if (!db.Database.IsSqlServer())
            throw new InvalidOperationException(
                "SQLite import requires SQL Server as the target database. " +
                "Start the server without --typ sqlite and configure a SQL Server connection string.");
    }

    public static IReadOnlyList<string> ValidateSourcePaths(IReadOnlyList<string> sourcePaths)
    {
        if (sourcePaths.Count == 0)
            throw new InvalidOperationException("At least one source SQLite path is required.");

        return sourcePaths.Select(ValidateSourcePath).ToList();
    }

    public static string ValidateSourcePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Source path cannot be empty.");

        if (!Path.IsPathRooted(path.Trim()))
            throw new InvalidOperationException($"Source path must be absolute: '{path}'.");

        if (path.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException($"Source path must not contain '..': '{path}'.");

        var fullPath = Path.GetFullPath(path.Trim());
        if (!fullPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Source path must end with .db: '{fullPath}'.");

        if (!File.Exists(fullPath))
            throw new InvalidOperationException($"SQLite file not found: '{fullPath}'.");

        return fullPath;
    }

    private static SqliteMemoryDbContext CreateReadOnlySourceContext(string path)
    {
        var connectionString = $"Data Source={path};Mode=ReadOnly";
        var options = new DbContextOptionsBuilder<SqliteMemoryDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new SqliteMemoryDbContext(options);
    }

    private static async Task ValidateSourceSchemaAsync(
        SqliteMemoryDbContext sourceDb,
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await sourceDb.Entities.AsNoTracking().CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Source file '{path}' is not a valid MemoryMCP SQLite database: {ex.Message}", ex);
        }
    }

    private static async Task<SourceSnapshot> LoadSourceSnapshotAsync(
        SqliteMemoryDbContext sourceDb,
        CancellationToken cancellationToken)
    {
        var entities = await sourceDb.Entities.AsNoTracking().ToListAsync(cancellationToken);
        var tokens = await sourceDb.Tokens.AsNoTracking().ToListAsync(cancellationToken);
        var memories = await sourceDb.Memories.AsNoTracking().ToListAsync(cancellationToken);
        var memoryEntities = await sourceDb.MemoryEntities.AsNoTracking().ToListAsync(cancellationToken);
        var memoryTokens = await sourceDb.MemoryTokens.AsNoTracking().ToListAsync(cancellationToken);
        var relationships = await sourceDb.EntityRelationships.AsNoTracking().ToListAsync(cancellationToken);

        return new SourceSnapshot(entities, tokens, memories, memoryEntities, memoryTokens, relationships);
    }

    private async Task<FileCounters> AnalyzeFileAsync(
        SourceSnapshot snapshot,
        SqliteImportOptions options,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var counters = new FileCounters();
        var entityById = snapshot.Entities.ToDictionary(e => e.Id);
        counters.MergedEntitiesInSource = snapshot.Entities.Count(e => e.Status == EntityStatus.Merged);
        if (counters.MergedEntitiesInSource > 0)
        {
            counters.Warnings.Add(
                $"{counters.MergedEntitiesInSource} merged entit(y/ies) in source will map to active targets (no merged stubs).");
        }

        var entityMap = new Dictionary<Guid, Guid>();
        var activeEntities = snapshot.Entities
            .Where(e => e.Status == EntityStatus.Active)
            .OrderBy(e => e.Name)
            .ToList();

        foreach (var src in activeEntities)
        {
            var targetId = await MapEntityAsync(src, options.ReuseEntities, counters, dryRun, cancellationToken);
            entityMap[src.Id] = targetId;
        }

        foreach (var src in snapshot.Entities.Where(e => e.Status == EntityStatus.Merged))
        {
            var resolvedId = ResolveSourceEntityId(src.Id, entityById);
            if (entityMap.TryGetValue(resolvedId, out var targetId))
                entityMap[src.Id] = targetId;
            else
                counters.Warnings.Add($"Merged entity '{src.Name}' could not be resolved in source.");
        }

        var memoriesToImport = await DetermineMemoriesToImportAsync(snapshot.Memories, options.SkipDuplicateRaw, cancellationToken);
        counters.MemoriesImported = memoriesToImport.Count;
        counters.MemoriesSkippedDuplicateRaw = snapshot.Memories.Count - memoriesToImport.Count;

        var memoryMap = new Dictionary<Guid, Guid>();
        if (!dryRun)
        {
            foreach (var src in memoriesToImport)
            {
                var newMemory = new Memory
                {
                    Id = Guid.NewGuid(),
                    Raw = src.Raw,
                    Created = src.Created,
                    Updated = src.Updated,
                    MemoryFrom = src.MemoryFrom,
                    Status = src.Status,
                    StatusNote = src.StatusNote
                };
                db.Memories.Add(newMemory);
                memoryMap[src.Id] = newMemory.Id;
            }

            foreach (var src in memoriesToImport)
            {
                if (!memoryMap.TryGetValue(src.Id, out var newId))
                    continue;

                var memory = db.Memories.Local.First(m => m.Id == newId);
                if (src.SupersedesMemoryId.HasValue &&
                    memoryMap.TryGetValue(src.SupersedesMemoryId.Value, out var supersedesId))
                {
                    memory.SupersedesMemoryId = supersedesId;
                }

                if (src.SupersededByMemoryId.HasValue &&
                    memoryMap.TryGetValue(src.SupersededByMemoryId.Value, out var supersededById))
                {
                    memory.SupersededByMemoryId = supersededById;
                }
            }
        }
        else
        {
            foreach (var src in memoriesToImport)
                memoryMap[src.Id] = Guid.NewGuid();
        }

        var importedMemoryIds = memoriesToImport.Select(m => m.Id).ToHashSet();
        var tokenMap = new Dictionary<Guid, Guid>();
        var tokensToMap = SelectTokensToMap(snapshot, importedMemoryIds);

        foreach (var src in tokensToMap.Where(t => t.Status == TokenStatus.Active))
        {
            var targetId = await MapActiveTokenAsync(src, options.ReuseTokens, counters, dryRun, cancellationToken);
            tokenMap[src.Id] = targetId;
        }

        foreach (var src in tokensToMap.Where(t => t.Status != TokenStatus.Active))
        {
            if (tokenMap.ContainsKey(src.Id))
                continue;

            if (dryRun)
            {
                tokenMap[src.Id] = Guid.NewGuid();
                counters.TokensNew++;
            }
            else
            {
                var copy = CopyToken(src);
                db.Tokens.Add(copy);
                tokenMap[src.Id] = copy.Id;
                counters.TokensNew++;
            }
        }

        var memoryEntityKeys = new HashSet<(Guid MemoryId, Guid EntityId)>();
        foreach (var link in snapshot.MemoryEntities.Where(me => importedMemoryIds.Contains(me.MemoryId)))
        {
            if (!memoryMap.TryGetValue(link.MemoryId, out var targetMemoryId))
                continue;
            if (!entityMap.TryGetValue(link.EntityId, out var targetEntityId))
                continue;

            var key = (targetMemoryId, targetEntityId);
            if (!memoryEntityKeys.Add(key))
                continue;

            if (!dryRun)
            {
                db.MemoryEntities.Add(new MemoryEntity
                {
                    MemoryId = targetMemoryId,
                    EntityId = targetEntityId
                });
            }
        }

        var memoryTokenKeys = new HashSet<(Guid MemoryId, Guid TokenId)>();
        foreach (var link in snapshot.MemoryTokens.Where(mt => importedMemoryIds.Contains(mt.MemoryId)))
        {
            if (!memoryMap.TryGetValue(link.MemoryId, out var targetMemoryId))
                continue;
            if (!tokenMap.TryGetValue(link.TokenId, out var targetTokenId))
                continue;

            var key = (targetMemoryId, targetTokenId);
            if (!memoryTokenKeys.Add(key))
                continue;

            if (!dryRun)
            {
                db.MemoryTokens.Add(new MemoryToken
                {
                    Id = Guid.NewGuid(),
                    MemoryId = targetMemoryId,
                    TokenId = targetTokenId
                });
            }
        }

        foreach (var rel in snapshot.Relationships)
        {
            if (!entityMap.TryGetValue(rel.FromEntityId, out var fromId))
            {
                counters.RelationshipsSkipped++;
                continue;
            }

            if (!entityMap.TryGetValue(rel.ToEntityId, out var toId))
            {
                counters.RelationshipsSkipped++;
                continue;
            }

            Guid? targetMemoryId = null;
            if (rel.MemoryId.HasValue)
            {
                if (!memoryMap.TryGetValue(rel.MemoryId.Value, out var mappedMemoryId))
                {
                    counters.RelationshipsSkipped++;
                    continue;
                }

                targetMemoryId = mappedMemoryId;
            }

            counters.RelationshipsImported++;
            if (!dryRun)
            {
                db.EntityRelationships.Add(new EntityRelationship
                {
                    Id = Guid.NewGuid(),
                    FromEntityId = fromId,
                    ToEntityId = toId,
                    RelationType = rel.RelationType,
                    MemoryId = targetMemoryId,
                    Confidence = rel.Confidence,
                    Created = rel.Created
                });
            }
        }

        return counters;
    }

    private async Task<IReadOnlyList<Memory>> DetermineMemoriesToImportAsync(
        IReadOnlyList<Memory> sourceMemories,
        bool skipDuplicateRaw,
        CancellationToken cancellationToken)
    {
        if (!skipDuplicateRaw)
            return sourceMemories;

        var sourceRaws = sourceMemories.Select(m => m.Raw).Distinct().ToList();
        var existingRaws = await db.Memories
            .AsNoTracking()
            .Where(m => sourceRaws.Contains(m.Raw))
            .Select(m => m.Raw)
            .ToListAsync(cancellationToken);

        var existingSet = existingRaws.ToHashSet(StringComparer.Ordinal);
        return sourceMemories.Where(m => !existingSet.Contains(m.Raw)).ToList();
    }

    private async Task<Guid> MapEntityAsync(
        Entity src,
        bool reuseEntities,
        FileCounters counters,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (reuseEntities)
        {
            if (dryRun)
            {
                var exists = await TargetEntityExistsAsync(src.Type, src.Name, cancellationToken);
                if (exists)
                {
                    counters.EntitiesReused++;
                    var existing = await FindTargetEntityAsync(src.Type, src.Name, cancellationToken);
                    return existing?.Id ?? Guid.NewGuid();
                }

                counters.EntitiesNew++;
                return Guid.NewGuid();
            }

            var entity = await entityResolution.ResolveOrCreateAsync(src.Type, src.Name, forceCreate: false, cancellationToken);
            if (db.Entry(entity).State == EntityState.Added)
                counters.EntitiesNew++;
            else
                counters.EntitiesReused++;

            return entity.Id;
        }

        if (dryRun)
        {
            counters.EntitiesNew++;
            return Guid.NewGuid();
        }

        var created = await entityResolution.ResolveOrCreateAsync(src.Type, src.Name, forceCreate: true, cancellationToken);
        counters.EntitiesNew++;
        return created.Id;
    }

    private async Task<Guid> MapActiveTokenAsync(
        Token src,
        bool reuseTokens,
        FileCounters counters,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var input = ToBundleInput(src);

        if (reuseTokens)
        {
            if (dryRun)
            {
                var exists = await TargetTokenExistsAsync(input, cancellationToken);
                if (exists)
                {
                    counters.TokensReused++;
                    var existing = await FindTargetTokenAsync(input, cancellationToken);
                    return existing?.Id ?? Guid.NewGuid();
                }

                counters.TokensNew++;
                return Guid.NewGuid();
            }

            var token = await tokenService.FindOrCreateAsync(input, cancellationToken);
            if (db.Entry(token).State == EntityState.Added)
                counters.TokensNew++;
            else
                counters.TokensReused++;

            return token.Id;
        }

        if (dryRun)
        {
            counters.TokensNew++;
            return Guid.NewGuid();
        }

        var created = CreateTokenWithoutSave(input);
        counters.TokensNew++;
        return created.Id;
    }

    private static IEnumerable<Token> SelectTokensToMap(SourceSnapshot snapshot, HashSet<Guid> importedMemoryIds)
    {
        var referencedTokenIds = snapshot.MemoryTokens
            .Where(mt => importedMemoryIds.Contains(mt.MemoryId))
            .Select(mt => mt.TokenId)
            .ToHashSet();

        return snapshot.Tokens
            .Where(t => t.Status == TokenStatus.Active || referencedTokenIds.Contains(t.Id))
            .OrderBy(t => t.Property)
            .ThenBy(t => t.SearchValue);
    }

    private static Guid ResolveSourceEntityId(Guid sourceId, IReadOnlyDictionary<Guid, Entity> entityById)
    {
        var visited = new HashSet<Guid>();
        var current = sourceId;

        while (true)
        {
            if (!visited.Add(current))
                throw new InvalidOperationException("Circular entity merge chain detected in source database.");

            if (!entityById.TryGetValue(current, out var entity))
                throw new InvalidOperationException($"Source entity {current} not found while resolving merge chain.");

            if (entity.Status != EntityStatus.Merged || entity.MergedIntoEntityId is null)
                return current;

            current = entity.MergedIntoEntityId.Value;
        }
    }

    private async Task<bool> TargetEntityExistsAsync(string type, string name, CancellationToken cancellationToken)
    {
        return await FindTargetEntityAsync(type, name, cancellationToken) is not null;
    }

    private async Task<Entity?> FindTargetEntityAsync(string type, string name, CancellationToken cancellationToken)
    {
        var normalizedType = type.Trim();
        var normalizedName = name.Trim();

        var exact = await db.Entities
            .AsNoTracking()
            .Where(e => e.Status == EntityStatus.Active)
            .FirstOrDefaultAsync(e => e.Type == normalizedType && e.Name == normalizedName, cancellationToken);

        if (exact is not null)
            return exact;

        return await db.Entities
            .AsNoTracking()
            .Where(e => e.Status == EntityStatus.Active)
            .FirstOrDefaultAsync(
                e => e.Type.ToLower() == normalizedType.ToLower() && e.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken);
    }

    private async Task<bool> TargetTokenExistsAsync(BundleTokenInput input, CancellationToken cancellationToken)
    {
        return await FindTargetTokenAsync(input, cancellationToken) is not null;
    }

    private async Task<Token?> FindTargetTokenAsync(BundleTokenInput input, CancellationToken cancellationToken)
    {
        var searchValue = TokenValueHelper.ComputeSearchValue(
            input.Type, input.IntValue, input.BoolValue, input.StringValue, input.FloatValue, input.DateTimeValue);

        return await db.Tokens
            .AsNoTracking()
            .Where(t => t.Status == TokenStatus.Active)
            .FirstOrDefaultAsync(
                t => t.Property == input.Property.Trim() &&
                     t.Type == input.Type &&
                     t.SearchValue == searchValue,
                cancellationToken);
    }

    private Token CreateTokenWithoutSave(BundleTokenInput input)
    {
        var token = new Token
        {
            Id = Guid.NewGuid(),
            Created = DateTime.UtcNow,
            Property = input.Property.Trim(),
            Confidence = input.Confidence,
            Source = input.Source,
            Status = TokenStatus.Active
        };

        TokenValueHelper.ApplyValues(
            token,
            input.Type,
            input.IntValue,
            input.BoolValue,
            input.StringValue,
            input.FloatValue,
            input.DateTimeValue);

        db.Tokens.Add(token);
        return token;
    }

    private static Token CopyToken(Token src)
    {
        var token = new Token
        {
            Id = Guid.NewGuid(),
            Created = src.Created,
            Updated = src.Updated,
            Status = src.Status,
            Property = src.Property,
            Confidence = src.Confidence,
            Source = src.Source,
            SupersedesTokenId = null,
            SupersededByTokenId = null
        };

        TokenValueHelper.ApplyValues(
            token,
            src.Type,
            src.IntValue,
            src.BoolValue,
            src.StringValue,
            src.FloatValue,
            src.DateTimeValue);

        return token;
    }

    private static BundleTokenInput ToBundleInput(Token token) =>
        new(
            token.Property,
            token.Type,
            token.IntValue,
            token.BoolValue,
            token.StringValue,
            token.FloatValue,
            token.DateTimeValue,
            token.Confidence,
            token.Source);

    private static SqliteImportFilePreview ToFilePreview(string path, FileCounters counters) =>
        new(
            path,
            counters.EntitiesReused,
            counters.EntitiesNew,
            counters.MergedEntitiesInSource,
            counters.TokensReused,
            counters.TokensNew,
            counters.MemoriesImported,
            counters.MemoriesSkippedDuplicateRaw,
            counters.RelationshipsImported,
            counters.RelationshipsSkipped,
            counters.Warnings);

    private static SqliteImportFileResult ToFileResult(string path, FileCounters counters) =>
        new(
            path,
            counters.EntitiesReused,
            counters.EntitiesNew,
            counters.TokensReused,
            counters.TokensNew,
            counters.MemoriesImported,
            counters.MemoriesSkippedDuplicateRaw,
            counters.RelationshipsImported,
            counters.RelationshipsSkipped);

    private static SqliteImportTotals SumFilePreviews(IReadOnlyList<SqliteImportFilePreview> files) =>
        new(
            files.Sum(f => f.EntitiesReused),
            files.Sum(f => f.EntitiesNew),
            files.Sum(f => f.TokensReused),
            files.Sum(f => f.TokensNew),
            files.Sum(f => f.MemoriesImported),
            files.Sum(f => f.MemoriesSkippedDuplicateRaw),
            files.Sum(f => f.RelationshipsImported),
            files.Sum(f => f.RelationshipsSkipped));

    private static SqliteImportTotals SumFileResults(IReadOnlyList<SqliteImportFileResult> files) =>
        new(
            files.Sum(f => f.EntitiesReused),
            files.Sum(f => f.EntitiesNew),
            files.Sum(f => f.TokensReused),
            files.Sum(f => f.TokensNew),
            files.Sum(f => f.MemoriesImported),
            files.Sum(f => f.MemoriesSkippedDuplicateRaw),
            files.Sum(f => f.RelationshipsImported),
            files.Sum(f => f.RelationshipsSkipped));
}
