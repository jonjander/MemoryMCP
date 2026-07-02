using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MemoryMCP.Data;

public class SqliteMemoryDbContextFactory : IDesignTimeDbContextFactory<SqliteMemoryDbContext>
{
    public SqliteMemoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteMemoryDbContext>();
        optionsBuilder.UseSqlite("Data Source=memory.db");
        return new SqliteMemoryDbContext(optionsBuilder.Options);
    }
}
