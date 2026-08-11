using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryMCP.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoryPartition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Partition",
                table: "Memories",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memories_Partition",
                table: "Memories",
                column: "Partition");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Memories_Partition",
                table: "Memories");

            migrationBuilder.DropColumn(
                name: "Partition",
                table: "Memories");
        }
    }
}
