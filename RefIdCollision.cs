using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MemoryMCP;

internal static class RefIdCollision
{
    public static bool IsRefUniqueViolation(DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is SqlException sql && sql.Number is 2601 or 2627)
                return sql.Message.Contains("Ref", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
