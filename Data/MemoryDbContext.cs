using Microsoft.EntityFrameworkCore;

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
}
