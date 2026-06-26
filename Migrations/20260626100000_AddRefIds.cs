using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryMCP.Migrations
{
    /// <inheritdoc />
    public partial class AddRefIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "Tokens",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "Memories",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "Entities",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Ref", table: "Tokens");
            migrationBuilder.DropColumn(name: "Ref", table: "Memories");
            migrationBuilder.DropColumn(name: "Ref", table: "Entities");
        }
    }
}
