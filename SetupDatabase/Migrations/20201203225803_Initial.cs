using Microsoft.EntityFrameworkCore.Migrations;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;

namespace SetupDatabase.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    LastSelectedCharacter = table.Column<int>(type: "integer", nullable: false),
                    SessionKey = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Password = table.Column<byte[]>(type: "bytea", nullable: false),
                    Mac = table.Column<string>(type: "CHAR(18)", nullable: false),
                    SoulCash = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "guilds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "account_posts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_posts_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    GateId = table.Column<int>(type: "integer", nullable: false),
                    SlotId = table.Column<byte>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Hero = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    Exp = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Inventory = table.Column<InventoryModel>(type: "jsonb", nullable: false),
                    Bank = table.Column<BankModel>(type: "jsonb", nullable: false),
                    PortraitId = table.Column<long>(type: "bigint", nullable: false),
                    Advancement = table.Column<byte>(type: "smallint", nullable: false),
                    Gesture = table.Column<long[]>(type: "bigint[]", nullable: false),
                    Appearance = table.Column<AppearanceModel>(type: "jsonb", nullable: false),
                    Place = table.Column<PlaceModel>(type: "jsonb", nullable: false),
                    LearnedSkill = table.Column<long[]>(type: "bigint[]", nullable: false),
                    QuickSlot = table.Column<long[]>(type: "bigint[]", nullable: false),
                    SkillSlot = table.Column<long[]>(type: "bigint[]", nullable: false),
                    Energy = table.Column<EnergyModel>(type: "jsonb", nullable: false),
                    Title = table.Column<TitleModel>(type: "jsonb", nullable: false),
                    Storage = table.Column<StorageModel[]>(type: "jsonb", nullable: false),
                    Profile = table.Column<ProfileModel>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_characters_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_posts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_character_posts_characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
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
                    TagId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_items_characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "Id", "LastSelectedCharacter", "Mac", "Nickname", "Password", "SessionKey", "SoulCash" },
                values: new object[,]
                {
                    { 1L, -1, "00-00-00-00-00-00", "sawich", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m, 0m },
                    { 2L, -1, "00-00-00-00-00-00", "Leeroy", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m, 0m },
                    { 3L, -1, "00-00-00-00-00-00", "Tweekly", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m, 0m },
                    { 4L, -1, "00-00-00-00-00-00", "Chelsea", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m, 0m },
                    { 5L, -1, "00-00-00-00-00-00", "Dez", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m, 0m },
                    { 6L, -1, "00-00-00-00-00-00", "Godo", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m, 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_posts_AccountId",
                table: "account_posts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_Nickname",
                table: "accounts",
                column: "Nickname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_SessionKey",
                table: "accounts",
                column: "SessionKey");

            migrationBuilder.CreateIndex(
                name: "IX_character_posts_CharacterId",
                table: "character_posts",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_characters_AccountId",
                table: "characters",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_characters_Name",
                table: "characters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_CharacterId",
                table: "items",
                column: "CharacterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_posts");

            migrationBuilder.DropTable(
                name: "character_posts");

            migrationBuilder.DropTable(
                name: "guilds");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "characters");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}