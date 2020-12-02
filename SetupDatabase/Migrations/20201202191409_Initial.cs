using System;
using Core.DatabaseSystem.Characters;
using Core.DatabaseSystem.Storages;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    LastSelectedCharacter = table.Column<int>(type: "integer", nullable: false),
                    SessionKey = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Password = table.Column<string>(type: "CHAR(64)", nullable: false),
                    Mac = table.Column<string>(type: "CHAR(18)", nullable: false),
                    SoulCash = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "guild",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "account_post",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_post", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_post_account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Gate = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<byte>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Character = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    Exp = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Inventory = table.Column<InventoryModel>(type: "jsonb", nullable: false),
                    Bank = table.Column<BankModel>(type: "jsonb", nullable: false),
                    Portrait = table.Column<long>(type: "bigint", nullable: false),
                    Advancement = table.Column<byte>(type: "smallint", nullable: false),
                    Gesture = table.Column<long[]>(type: "bigint[]", nullable: false),
                    Appearance = table.Column<ApperanceModel>(type: "jsonb", nullable: false),
                    WorldPosition = table.Column<PositionModel>(type: "jsonb", nullable: false),
                    LearnedSkill = table.Column<long[]>(type: "bigint[]", nullable: false),
                    QuickSlot = table.Column<long[]>(type: "bigint[]", nullable: false),
                    SkillSlot = table.Column<long[]>(type: "bigint[]", nullable: false),
                    Energy = table.Column<EnergyModel>(type: "jsonb", nullable: false),
                    Title = table.Column<TitleModel>(type: "jsonb", nullable: false),
                    Storage = table.Column<StorageModel[]>(type: "jsonb", nullable: false),
                    Profile = table.Column<ProfileModel>(type: "jsonb", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character", x => x.Id);
                    table.ForeignKey(
                        name: "FK_character_account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_post",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_post", x => x.Id);
                    table.ForeignKey(
                        name: "FK_character_post_character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "storage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    PrototypeId = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    SlotId = table.Column<int>(type: "integer", nullable: false),
                    StorageType = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    Durability = table.Column<byte>(type: "smallint", nullable: false),
                    Slots = table.Column<byte>(type: "smallint", nullable: false),
                    Quantly = table.Column<byte>(type: "smallint", nullable: false),
                    Upgrade = table.Column<UpgradeModel>(type: "jsonb", nullable: false),
                    Color = table.Column<long>(type: "bigint", nullable: false),
                    DyeColor = table.Column<long>(type: "bigint", nullable: false),
                    Stats = table.Column<StatModel[]>(type: "jsonb", nullable: false),
                    BroochSlots = table.Column<string>(type: "char(15)", nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_storage_character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "Id", "LastSelectedCharacter", "Mac", "Nickname", "Password", "SessionKey", "SoulCash" },
                values: new object[,]
                {
                    { 1L, -1, "00-00-00-00-00-00", "sawich", "VaVWDzfVP8ot1N4QMr3D4bSicnxcr3DYCtfLMPcVr6/kFbPr2jxw2oKGc05UqeqPAuZPoRpyMh42cLNa3b88+g==", 0m, 0m },
                    { 2L, -1, "00-00-00-00-00-00", "Leeroy", "VaVWDzfVP8ot1N4QMr3D4bSicnxcr3DYCtfLMPcVr6/kFbPr2jxw2oKGc05UqeqPAuZPoRpyMh42cLNa3b88+g==", 0m, 0m },
                    { 3L, -1, "00-00-00-00-00-00", "Tweekly", "VaVWDzfVP8ot1N4QMr3D4bSicnxcr3DYCtfLMPcVr6/kFbPr2jxw2oKGc05UqeqPAuZPoRpyMh42cLNa3b88+g==", 0m, 0m },
                    { 4L, -1, "00-00-00-00-00-00", "Chelsea", "VaVWDzfVP8ot1N4QMr3D4bSicnxcr3DYCtfLMPcVr6/kFbPr2jxw2oKGc05UqeqPAuZPoRpyMh42cLNa3b88+g==", 0m, 0m },
                    { 5L, -1, "00-00-00-00-00-00", "Dez", "VaVWDzfVP8ot1N4QMr3D4bSicnxcr3DYCtfLMPcVr6/kFbPr2jxw2oKGc05UqeqPAuZPoRpyMh42cLNa3b88+g==", 0m, 0m },
                    { 6L, -1, "00-00-00-00-00-00", "Godo", "VaVWDzfVP8ot1N4QMr3D4bSicnxcr3DYCtfLMPcVr6/kFbPr2jxw2oKGc05UqeqPAuZPoRpyMh42cLNa3b88+g==", 0m, 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_Nickname",
                table: "account",
                column: "Nickname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_SessionKey",
                table: "account",
                column: "SessionKey");

            migrationBuilder.CreateIndex(
                name: "IX_account_post_AccountId",
                table: "account_post",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_character_AccountId",
                table: "character",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_character_Name",
                table: "character",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_character_post_CharacterId",
                table: "character_post",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_storage_CharacterId",
                table: "storage",
                column: "CharacterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_post");

            migrationBuilder.DropTable(
                name: "character_post");

            migrationBuilder.DropTable(
                name: "guild");

            migrationBuilder.DropTable(
                name: "storage");

            migrationBuilder.DropTable(
                name: "character");

            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
