using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryMCP.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityTokenUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entities_Type_Name",
                table: "Entities");

            migrationBuilder.AddColumn<Guid>(
                name: "MergedIntoEntityId",
                table: "Entities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Entities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Updated",
                table: "Entities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByTokenId",
                table: "Tokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesTokenId",
                table: "Tokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Updated",
                table: "Tokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntityRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionType = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    NewName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PreviousType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    NewType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityRevisions_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionType = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousProperty = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NewProperty = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PreviousType = table.Column<int>(type: "int", nullable: true),
                    NewType = table.Column<int>(type: "int", nullable: true),
                    PreviousSearchValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    NewSearchValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PreviousConfidence = table.Column<float>(type: "real", nullable: true),
                    NewConfidence = table.Column<float>(type: "real", nullable: true),
                    PreviousSource = table.Column<int>(type: "int", nullable: true),
                    NewSource = table.Column<int>(type: "int", nullable: true),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: true),
                    SuccessorTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenRevisions_Tokens_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entities_MergedIntoEntityId",
                table: "Entities",
                column: "MergedIntoEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Status",
                table: "Entities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Type_Name",
                table: "Entities",
                columns: new[] { "Type", "Name" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Status",
                table: "Tokens",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_SupersededByTokenId",
                table: "Tokens",
                column: "SupersededByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_SupersedesTokenId",
                table: "Tokens",
                column: "SupersedesTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRevisions_Created",
                table: "EntityRevisions",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRevisions_EntityId",
                table: "EntityRevisions",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenRevisions_Created",
                table: "TokenRevisions",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_TokenRevisions_TokenId",
                table: "TokenRevisions",
                column: "TokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Entities_Entities_MergedIntoEntityId",
                table: "Entities",
                column: "MergedIntoEntityId",
                principalTable: "Entities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tokens_Tokens_SupersededByTokenId",
                table: "Tokens",
                column: "SupersededByTokenId",
                principalTable: "Tokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tokens_Tokens_SupersedesTokenId",
                table: "Tokens",
                column: "SupersedesTokenId",
                principalTable: "Tokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entities_Entities_MergedIntoEntityId",
                table: "Entities");

            migrationBuilder.DropForeignKey(
                name: "FK_Tokens_Tokens_SupersededByTokenId",
                table: "Tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Tokens_Tokens_SupersedesTokenId",
                table: "Tokens");

            migrationBuilder.DropTable(
                name: "EntityRevisions");

            migrationBuilder.DropTable(
                name: "TokenRevisions");

            migrationBuilder.DropIndex(
                name: "IX_Entities_MergedIntoEntityId",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_Entities_Status",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_Entities_Type_Name",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_Status",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_SupersededByTokenId",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_SupersededByTokenId",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "MergedIntoEntityId",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Updated",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "SupersededByTokenId",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "SupersedesTokenId",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "Updated",
                table: "Tokens");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Type_Name",
                table: "Entities",
                columns: new[] { "Type", "Name" },
                unique: true);
        }
    }
}
