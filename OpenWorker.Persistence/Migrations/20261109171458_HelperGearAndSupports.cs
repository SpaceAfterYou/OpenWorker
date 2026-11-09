using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HelperGearAndSupports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelperSupports",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    SupportType = table.Column<byte>(type: "smallint", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false),
                    Until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reward = table.Column<byte>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelperSupports", x => x.PersonId);
                    table.ForeignKey(
                        name: "FK_HelperSupports_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HelperSupportUses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupporterId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelperSupportUses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelperSupportUses_Persons_SupporterId",
                        column: x => x.SupporterId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HelperSupportUses_Persons_UserId",
                        column: x => x.UserId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_HelperPlace",
                table: "Items",
                columns: new[] { "PersonId", "Slot" },
                unique: true,
                filter: "\"Storage\" = 105");

            migrationBuilder.CreateIndex(
                name: "IX_HelperSupportUses_SupporterId_UserId_Until",
                table: "HelperSupportUses",
                columns: new[] { "SupporterId", "UserId", "Until" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HelperSupportUses_UserId",
                table: "HelperSupportUses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelperSupports");

            migrationBuilder.DropTable(
                name: "HelperSupportUses");

            migrationBuilder.DropIndex(
                name: "IX_Items_HelperPlace",
                table: "Items");
        }
    }
}
