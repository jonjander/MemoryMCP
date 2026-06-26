using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MemoryMCP.Data;

public class MemoryDbContext(DbContextOptions<MemoryDbContext> options) : DbContext(options)
{
    public DbSet<Memory> Memories => Set<Memory>();
    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<MemoryEntity> MemoryEntities => Set<MemoryEntity>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<MemoryToken> MemoryTokens => Set<MemoryToken>();
    public DbSet<EntityRelationship> EntityRelationships => Set<EntityRelationship>();
    public DbSet<MemoryRevision> MemoryRevisions => Set<MemoryRevision>();
    public DbSet<EntityRevision> EntityRevisions => Set<EntityRevision>();
    public DbSet<TokenRevision> TokenRevisions => Set<TokenRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MemoryDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignMissingRefs();
        return SaveChangesWithRefRetry(() => base.SaveChanges(acceptAllChangesOnSuccess));
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AssignMissingRefs();
        return await SaveChangesWithRefRetryAsync(
            () => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken),
            cancellationToken);
    }

    private void AssignMissingRefs()
    {
        AssignMissingRefs(ChangeTracker.Entries<Entity>());
        AssignMissingRefs(ChangeTracker.Entries<Memory>());
        AssignMissingRefs(ChangeTracker.Entries<Token>());
    }

    private static void AssignMissingRefs<T>(IEnumerable<EntityEntry<T>> entries) where T : class, IHasRef
    {
        foreach (var entry in entries.Where(e => e.State == EntityState.Added && string.IsNullOrEmpty(e.Entity.Ref)))
            entry.Entity.Ref = RefIdGenerator.New();
    }

    private void RegenerateRefsForAddedEntries()
    {
        RegenerateRefs(ChangeTracker.Entries<Entity>());
        RegenerateRefs(ChangeTracker.Entries<Memory>());
        RegenerateRefs(ChangeTracker.Entries<Token>());
    }

    private static void RegenerateRefs<T>(IEnumerable<EntityEntry<T>> entries) where T : class, IHasRef
    {
        foreach (var entry in entries.Where(e => e.State == EntityState.Added))
            entry.Entity.Ref = RefIdGenerator.New();
    }

    private int SaveChangesWithRefRetry(Func<int> save)
    {
        const int maxAttempts = 12;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return save();
            }
            catch (DbUpdateException ex) when (RefIdCollision.IsRefUniqueViolation(ex) && attempt < maxAttempts - 1)
            {
                RegenerateRefsForAddedEntries();
            }
        }

        throw new InvalidOperationException("Could not allocate unique Ref id after multiple attempts.");
    }

    private async Task<int> SaveChangesWithRefRetryAsync(Func<Task<int>> save, CancellationToken cancellationToken)
    {
        const int maxAttempts = 12;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await save();
            }
            catch (DbUpdateException ex) when (RefIdCollision.IsRefUniqueViolation(ex) && attempt < maxAttempts - 1)
            {
                RegenerateRefsForAddedEntries();
            }
        }

        throw new InvalidOperationException("Could not allocate unique Ref id after multiple attempts.");
    }
}
