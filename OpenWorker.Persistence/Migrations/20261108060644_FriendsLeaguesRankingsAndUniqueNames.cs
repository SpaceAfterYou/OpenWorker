using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FriendsLeaguesRankingsAndUniqueNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leagues_Persons_MasterId",
                table: "Leagues");

            migrationBuilder.DropForeignKey(
                name: "FK_Persons_Leagues_LeagueId",
                table: "Persons");

            migrationBuilder.DropTable(
                name: "PersonPersistentPersonPersistent");

            // Leagues of the old layout lose their members with Persons.LeagueId and have no rights
            // or limits: the server cannot use them, and a name longer than 8 would stop the column change.
            migrationBuilder.Sql("DELETE FROM \"Leagues\";");

            migrationBuilder.DropIndex(
                name: "IX_Persons_LeagueId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Persons");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogoutAt",
                table: "Persons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeagueCreateBlockedUntil",
                table: "Persons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeagueJoinBlockedUntil",
                table: "Persons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int[]>(
                name: "TitleList",
                table: "Persons",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Leagues",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(12)",
                oldMaxLength: 12);

            migrationBuilder.AddColumn<int[]>(
                name: "Auth",
                table: "Leagues",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<long>(
                name: "Exp",
                table: "Leagues",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int[]>(
                name: "LimitGoldOut",
                table: "Leagues",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<long>(
                name: "Money",
                table: "Leagues",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Notice",
                table: "Leagues",
                type: "character varying(800)",
                maxLength: 800,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NoticeAt",
                table: "Leagues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Open",
                table: "Leagues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string[]>(
                name: "PositionNames",
                table: "Leagues",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<byte>(
                name: "Rating",
                table: "Leagues",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "RecruitNotice",
                table: "Leagues",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RecruitNoticeAt",
                table: "Leagues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Friends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    Point = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Friends_Persons_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Friends_Persons_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueApplicants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueApplicants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueApplicants_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueApplicants_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueBoards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueBoards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueBoards_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueBoards_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueMembers",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<byte>(type: "smallint", nullable: false),
                    Exp = table.Column<long>(type: "bigint", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueMembers", x => x.PersonId);
                    table.ForeignKey(
                        name: "FK_LeagueMembers_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueMembers_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RankingEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Time = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankingEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankingEntries_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Name",
                table: "Persons",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_Name",
                table: "Leagues",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friends_OwnerId_TargetId",
                table: "Friends",
                columns: new[] { "OwnerId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friends_TargetId",
                table: "Friends",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueApplicants_LeagueId_PersonId",
                table: "LeagueApplicants",
                columns: new[] { "LeagueId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeagueApplicants_PersonId",
                table: "LeagueApplicants",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueBoards_LeagueId",
                table: "LeagueBoards",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueBoards_PersonId",
                table: "LeagueBoards",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMembers_LeagueId",
                table: "LeagueMembers",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_RankingEntries_Kind_Scope_PersonId",
                table: "RankingEntries",
                columns: new[] { "Kind", "Scope", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RankingEntries_PersonId",
                table: "RankingEntries",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Friends");

            migrationBuilder.DropTable(
                name: "LeagueApplicants");

            migrationBuilder.DropTable(
                name: "LeagueBoards");

            migrationBuilder.DropTable(
                name: "LeagueMembers");

            migrationBuilder.DropTable(
                name: "RankingEntries");

            migrationBuilder.DropIndex(
                name: "IX_Persons_Name",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_Leagues_Name",
                table: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Username",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastLogoutAt",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "LeagueCreateBlockedUntil",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "LeagueJoinBlockedUntil",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "TitleList",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Auth",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Exp",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "LimitGoldOut",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Money",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Notice",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "NoticeAt",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Open",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PositionNames",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "RecruitNotice",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "RecruitNoticeAt",
                table: "Leagues");

            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Persons",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Leagues",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.CreateTable(
                name: "PersonPersistentPersonPersistent",
                columns: table => new
                {
                    BlockedListId = table.Column<int>(type: "integer", nullable: false),
                    FriendListId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonPersistentPersonPersistent", x => new { x.BlockedListId, x.FriendListId });
                    table.ForeignKey(
                        name: "FK_PersonPersistentPersonPersistent_Persons_BlockedListId",
                        column: x => x.BlockedListId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonPersistentPersonPersistent_Persons_FriendListId",
                        column: x => x.FriendListId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Persons_LeagueId",
                table: "Persons",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonPersistentPersonPersistent_FriendListId",
                table: "PersonPersistentPersonPersistent",
                column: "FriendListId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leagues_Persons_MasterId",
                table: "Leagues",
                column: "MasterId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_Leagues_LeagueId",
                table: "Persons",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id");
        }
    }
}
