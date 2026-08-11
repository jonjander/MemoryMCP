namespace MemoryMCP.Services;

public static class MemoryQueryExtensions
{
    public static IQueryable<Memory> WhereActive(this IQueryable<Memory> query, bool includeInactive) =>
        includeInactive ? query : query.Where(m => m.Status == MemoryStatus.Active);

    /// <summary>
    /// When <paramref name="partition"/> is set, only memories with that exact key.
    /// When null/empty, no filter — all partitions including null are visible.
    /// </summary>
    public static IQueryable<Memory> InPartition(this IQueryable<Memory> query, string? partition) =>
        string.IsNullOrEmpty(partition) ? query : query.Where(m => m.Partition == partition);
}
