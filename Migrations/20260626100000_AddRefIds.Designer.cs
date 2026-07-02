using MemoryMCP.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryMCP.Migrations
{
    [DbContext(typeof(MemoryDbContext))]
    [Migration("20260626100000_AddRefIds")]
    public partial class AddRefIds
    {
    }
}
