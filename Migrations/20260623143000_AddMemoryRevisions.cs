using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryMCP.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoryRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Memories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StatusNote",
                table: "Memories",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesMemoryId",
                table: "Memories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByMemoryId",
                table: "Memories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemoryRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionType = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousMemoryFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewMemoryFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: true),
                    SuccessorMemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuccessorRaw = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryRevisions_Memories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Memories_MemoryFrom",
                table: "Memories",
                column: "MemoryFrom");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_Status",
                table: "Memories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_SupersededByMemoryId",
                table: "Memories",
                column: "SupersededByMemoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_SupersedesMemoryId",
                table: "Memories",
                column: "SupersedesMemoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryRevisions_Created",
                table: "MemoryRevisions",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryRevisions_MemoryId",
                table: "MemoryRevisions",
                column: "MemoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Memories_Memories_SupersededByMemoryId",
                table: "Memories",
                column: "SupersededByMemoryId",
                principalTable: "Memories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Memories_Memories_SupersedesMemoryId",
                table: "Memories",
                column: "SupersedesMemoryId",
                principalTable: "Memories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Memories_Memories_SupersededByMemoryId",
                table: "Memories");

            migrationBuilder.DropForeignKey(
                name: "FK_Memories_Memories_SupersedesMemoryId",
                table: "Memories");

            migrationBuilder.DropTable(
                name: "MemoryRevisions");

            migrationBuilder.DropIndex(
                name: "IX_Memories_MemoryFrom",
                table: "Memories");

            migrationBuilder.DropIndex(
                name: "IX_Memories_Status",
                table: "Memories");

            migrationBuilder.DropIndex(
                name: "IX_Memories_SupersededByMemoryId",
                table: "Memories");

            migrationBuilder.DropIndex(
                name: "IX_Memories_SupersedesMemoryId",
                table: "Memories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Memories");

            migrationBuilder.DropColumn(
                name: "StatusNote",
                table: "Memories");

            migrationBuilder.DropColumn(
                name: "SupersedesMemoryId",
                table: "Memories");

            migrationBuilder.DropColumn(
                name: "SupersededByMemoryId",
                table: "Memories");
        }
    }
}
