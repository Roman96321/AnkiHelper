using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_training_statistics",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    LearningNoteCounts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_training_statistics", x => new { x.UserId, x.Date });
                });

            migrationBuilder.CreateTable(
                name: "deck_import_settings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckId = table.Column<long>(type: "bigint", nullable: false),
                    DeckName = table.Column<string>(type: "character varying(248)", maxLength: 248, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deck_import_settings", x => new { x.UserId, x.DeckId });
                });

            migrationBuilder.CreateTable(
                name: "decks",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FrontLanguage = table.Column<string>(type: "text", nullable: false),
                    BackLanguage = table.Column<string>(type: "text", nullable: false),
                    DeckDailyNewNotesCount = table.Column<int>(type: "integer", nullable: false),
                    UseOnlyLearnedNotes = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decks", x => new { x.UserId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "deck_import_field_mappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckId = table.Column<long>(type: "bigint", nullable: false),
                    FrontKey = table.Column<string>(type: "character varying(248)", maxLength: 248, nullable: false),
                    BackKey = table.Column<string>(type: "character varying(248)", maxLength: 248, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deck_import_field_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deck_import_field_mappings_deck_import_settings_UserId_Deck~",
                        columns: x => new { x.UserId, x.DeckId },
                        principalTable: "deck_import_settings",
                        principalColumns: new[] { "UserId", "DeckId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_new_note_progress",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckId = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    NewAnsweredCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_new_note_progress", x => new { x.UserId, x.DeckId, x.Date });
                    table.ForeignKey(
                        name: "FK_daily_new_note_progress_decks_UserId_DeckId",
                        columns: x => new { x.UserId, x.DeckId },
                        principalTable: "decks",
                        principalColumns: new[] { "UserId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DeckId = table.Column<long>(type: "bigint", nullable: false),
                    Front = table.Column<string>(type: "text", nullable: false),
                    Back = table.Column<string>(type: "text", nullable: false),
                    IsLearned = table.Column<bool>(type: "boolean", nullable: false),
                    LearningPoints = table.Column<int>(type: "integer", nullable: false),
                    NextReviewDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => new { x.UserId, x.Id });
                    table.ForeignKey(
                        name: "FK_notes_decks_UserId_DeckId",
                        columns: x => new { x.UserId, x.DeckId },
                        principalTable: "decks",
                        principalColumns: new[] { "UserId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deck_import_field_mappings_UserId_DeckId",
                table: "deck_import_field_mappings",
                columns: new[] { "UserId", "DeckId" });

            migrationBuilder.CreateIndex(
                name: "IX_deck_import_settings_UserId",
                table: "deck_import_settings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_decks_UserId",
                table: "decks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_notes_UserId_DeckId",
                table: "notes",
                columns: new[] { "UserId", "DeckId" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_new_note_progress");

            migrationBuilder.DropTable(
                name: "daily_training_statistics");

            migrationBuilder.DropTable(
                name: "deck_import_field_mappings");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "deck_import_settings");

            migrationBuilder.DropTable(
                name: "decks");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
