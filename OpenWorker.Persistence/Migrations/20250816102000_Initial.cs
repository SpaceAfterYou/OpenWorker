using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Discord = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "bytea", maxLength: 64, nullable: false),
                    SaltHash = table.Column<byte[]>(type: "bytea", maxLength: 16, nullable: false),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TradePassword = table.Column<string>(type: "text", nullable: false),
                    SecondPassword = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gates",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardEmblem = table.Column<int>(type: "integer", nullable: false),
                    CardBorder = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    MasterId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Hero = table.Column<int>(type: "integer", nullable: false),
                    DefaultHairStyle = table.Column<short>(type: "smallint", nullable: false),
                    DefaultHairColor = table.Column<short>(type: "smallint", nullable: false),
                    DefaultEyeColor = table.Column<short>(type: "smallint", nullable: false),
                    DefaultSkinColor = table.Column<short>(type: "smallint", nullable: false),
                    EquippedHairStyle = table.Column<short>(type: "smallint", nullable: false),
                    EquippedHairColor = table.Column<short>(type: "smallint", nullable: false),
                    EquippedEyeColor = table.Column<short>(type: "smallint", nullable: false),
                    EquippedSkinColor = table.Column<short>(type: "smallint", nullable: false),
                    TitlePrimary = table.Column<long>(type: "bigint", nullable: false),
                    TitleSecondary = table.Column<long>(type: "bigint", nullable: false),
                    FatiguePointCommon = table.Column<int>(type: "integer", nullable: false),
                    FatiguePointBonus = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    GateId = table.Column<short>(type: "smallint", nullable: false),
                    LeagueId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persons_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Persons_Gates_GateId",
                        column: x => x.GateId,
                        principalTable: "Gates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Persons_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_MasterId",
                table: "Leagues",
                column: "MasterId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_AccountId",
                table: "Persons",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_GateId",
                table: "Persons",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_LeagueId",
                table: "Persons",
                column: "LeagueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leagues_Persons_MasterId",
                table: "Leagues",
                column: "MasterId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leagues_Persons_MasterId",
                table: "Leagues");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Gates");

            migrationBuilder.DropTable(
                name: "Leagues");
        }
    }
}
