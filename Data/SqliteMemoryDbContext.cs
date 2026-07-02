using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Data;

public class SqliteMemoryDbContext(DbContextOptions<SqliteMemoryDbContext> options)
    : MemoryDbContext(options);
