using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryMCP.Migrations.Sqlite
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
                type: "TEXT",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "Memories",
                type: "TEXT",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "Entities",
                type: "TEXT",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Ref",
                table: "Tokens",
                column: "Ref",
                unique: true,
                filter: "[Ref] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_Ref",
                table: "Memories",
                column: "Ref",
                unique: true,
                filter: "[Ref] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Ref",
                table: "Entities",
                column: "Ref",
                unique: true,
                filter: "[Ref] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tokens_Ref",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Memories_Ref",
                table: "Memories");

            migrationBuilder.DropIndex(
                name: "IX_Entities_Ref",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "Memories");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "Entities");
        }
    }
}
