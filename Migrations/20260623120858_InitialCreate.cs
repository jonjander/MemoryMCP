using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryMCP.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Memories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Raw = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Updated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemoryFrom = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IntValue = table.Column<int>(type: "int", nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    StringValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    FloatValue = table.Column<float>(type: "real", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Property = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SearchValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityRelationships_Entities_FromEntityId",
                        column: x => x.FromEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntityRelationships_Entities_ToEntityId",
                        column: x => x.ToEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntityRelationships_Memories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MemoryEntities",
                columns: table => new
                {
                    MemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryEntities", x => new { x.MemoryId, x.EntityId });
                    table.ForeignKey(
                        name: "FK_MemoryEntities_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemoryEntities_Memories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryTokens_Memories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemoryTokens_Tokens_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Name",
                table: "Entities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Type",
                table: "Entities",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Type_Name",
                table: "Entities",
                columns: new[] { "Type", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelationships_FromEntityId_RelationType",
                table: "EntityRelationships",
                columns: new[] { "FromEntityId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelationships_FromEntityId_ToEntityId_RelationType",
                table: "EntityRelationships",
                columns: new[] { "FromEntityId", "ToEntityId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelationships_MemoryId",
                table: "EntityRelationships",
                column: "MemoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelationships_ToEntityId_RelationType",
                table: "EntityRelationships",
                columns: new[] { "ToEntityId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_Memories_Created",
                table: "Memories",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryEntities_EntityId",
                table: "MemoryEntities",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryTokens_MemoryId_TokenId",
                table: "MemoryTokens",
                columns: new[] { "MemoryId", "TokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemoryTokens_TokenId",
                table: "MemoryTokens",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Property_IntValue",
                table: "Tokens",
                columns: new[] { "Property", "IntValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Property_SearchValue",
                table: "Tokens",
                columns: new[] { "Property", "SearchValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Property_StringValue",
                table: "Tokens",
                columns: new[] { "Property", "StringValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Property_Type_SearchValue",
                table: "Tokens",
                columns: new[] { "Property", "Type", "SearchValue" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityRelationships");

            migrationBuilder.DropTable(
                name: "MemoryEntities");

            migrationBuilder.DropTable(
                name: "MemoryTokens");

            migrationBuilder.DropTable(
                name: "Entities");

            migrationBuilder.DropTable(
                name: "Memories");

            migrationBuilder.DropTable(
                name: "Tokens");
        }
    }
}
