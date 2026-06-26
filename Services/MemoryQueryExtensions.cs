namespace MemoryMCP.Services;

public static class MemoryQueryExtensions
{
    public static IQueryable<Memory> WhereActive(this IQueryable<Memory> query, bool includeInactive) =>
        includeInactive ? query : query.Where(m => m.Status == MemoryStatus.Active);
}
