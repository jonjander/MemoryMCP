using MemoryMCP.Data;
using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Services;

public class RefIdResolver(MemoryDbContext db)
{
    public const string IdOrRefDescription =
        "Primary: short Ref id (8-char Base64) from prior responses — preferred to save context. " +
        "Backward compatible: full Guid also accepted. Never ask the user; use Ref from store/search results when available.";

    public async Task<Guid> ResolveEntityIdAsync(string idOrRef, CancellationToken cancellationToken = default) =>
        await ResolveAsync(
            idOrRef,
            () => db.Entities.AsNoTracking().Where(e => e.Ref == idOrRef).Select(e => e.Id).FirstOrDefaultAsync(cancellationToken),
            "Entity",
            cancellationToken);

    public async Task<Guid> ResolveMemoryIdAsync(string idOrRef, CancellationToken cancellationToken = default) =>
        await ResolveAsync(
            idOrRef,
            () => db.Memories.AsNoTracking().Where(m => m.Ref == idOrRef).Select(m => m.Id).FirstOrDefaultAsync(cancellationToken),
            "Memory",
            cancellationToken);

    public async Task<Guid> ResolveTokenIdAsync(string idOrRef, CancellationToken cancellationToken = default) =>
        await ResolveAsync(
            idOrRef,
            () => db.Tokens.AsNoTracking().Where(t => t.Ref == idOrRef).Select(t => t.Id).FirstOrDefaultAsync(cancellationToken),
            "Token",
            cancellationToken);

    private static async Task<Guid> ResolveAsync(
        string idOrRef,
        Func<Task<Guid>> lookupByRef,
        string kind,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idOrRef))
            throw new InvalidOperationException($"{kind} id is required.");

        var trimmed = idOrRef.Trim();
        if (Guid.TryParse(trimmed, out var guid))
            return guid;

        if (!RefIdGenerator.IsValidFormat(trimmed))
            throw new InvalidOperationException($"Invalid {kind.ToLowerInvariant()} id '{trimmed}'. Use Ref ({RefIdGenerator.CharLength} chars) or Guid.");

        var id = await lookupByRef();
        if (id == Guid.Empty)
            throw new InvalidOperationException($"{kind} not found: {trimmed}");

        return id;
    }
}
