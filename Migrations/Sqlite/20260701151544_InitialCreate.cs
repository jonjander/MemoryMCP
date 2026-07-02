using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryMCP.Migrations.Sqlite
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    MergedIntoEntityId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entities_Entities_MergedIntoEntityId",
                        column: x => x.MergedIntoEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Memories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Raw = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MemoryFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusNote = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SupersedesMemoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SupersededByMemoryId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memories_Memories_SupersededByMemoryId",
                        column: x => x.SupersededByMemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Memories_Memories_SupersedesMemoryId",
                        column: x => x.SupersedesMemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SupersedesTokenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SupersededByTokenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IntValue = table.Column<int>(type: "INTEGER", nullable: true),
                    BoolValue = table.Column<bool>(type: "INTEGER", nullable: true),
                    StringValue = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    FloatValue = table.Column<float>(type: "REAL", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Property = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    SearchValue = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tokens_Tokens_SupersededByTokenId",
                        column: x => x.SupersededByTokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tokens_Tokens_SupersedesTokenId",
                        column: x => x.SupersedesTokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntityRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PreviousName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    NewName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    PreviousType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    NewType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    PreviousStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    NewStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "TEXT", nullable: true)
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
                name: "EntityRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    MemoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    MemoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                name: "MemoryRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PreviousMemoryFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NewMemoryFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PreviousStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    NewStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    SuccessorMemoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SuccessorRaw = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "MemoryTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenId = table.Column<Guid>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "TokenRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PreviousProperty = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NewProperty = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PreviousType = table.Column<int>(type: "INTEGER", nullable: true),
                    NewType = table.Column<int>(type: "INTEGER", nullable: true),
                    PreviousSearchValue = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    NewSearchValue = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    PreviousConfidence = table.Column<float>(type: "REAL", nullable: true),
                    NewConfidence = table.Column<float>(type: "REAL", nullable: true),
                    PreviousSource = table.Column<int>(type: "INTEGER", nullable: true),
                    NewSource = table.Column<int>(type: "INTEGER", nullable: true),
                    PreviousStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    NewStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    SuccessorTokenId = table.Column<Guid>(type: "TEXT", nullable: true)
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
                name: "IX_Entities_Name",
                table: "Entities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Status",
                table: "Entities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Type",
                table: "Entities",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Type_Name",
                table: "Entities",
                columns: new[] { "Type", "Name" },
                unique: true,
                filter: "[Status] = 0");

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
                name: "IX_EntityRevisions_Created",
                table: "EntityRevisions",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRevisions_EntityId",
                table: "EntityRevisions",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_Created",
                table: "Memories",
                column: "Created");

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
                name: "IX_MemoryEntities_EntityId",
                table: "MemoryEntities",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryRevisions_Created",
                table: "MemoryRevisions",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryRevisions_MemoryId",
                table: "MemoryRevisions",
                column: "MemoryId");

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
                name: "IX_TokenRevisions_Created",
                table: "TokenRevisions",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_TokenRevisions_TokenId",
                table: "TokenRevisions",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityRelationships");

            migrationBuilder.DropTable(
                name: "EntityRevisions");

            migrationBuilder.DropTable(
                name: "MemoryEntities");

            migrationBuilder.DropTable(
                name: "MemoryRevisions");

            migrationBuilder.DropTable(
                name: "MemoryTokens");

            migrationBuilder.DropTable(
                name: "TokenRevisions");

            migrationBuilder.DropTable(
                name: "Entities");

            migrationBuilder.DropTable(
                name: "Memories");

            migrationBuilder.DropTable(
                name: "Tokens");
        }
    }
}
