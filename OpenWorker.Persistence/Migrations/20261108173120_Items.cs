using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Items : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "ItemSerial",
                startValue: 1000L);

            migrationBuilder.AddColumn<long>(
                name: "BattlePoint",
                table: "Persons",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Ether",
                table: "Persons",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Experience",
                table: "Persons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "Gold",
                table: "Persons",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte>(
                name: "Level",
                table: "Persons",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<int[]>(
                name: "QuickSlotCards",
                table: "Persons",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "QuickSlotItems",
                table: "Persons",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "StorageGrades",
                table: "Persons",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Storage = table.Column<byte>(type: "smallint", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Item = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<short>(type: "smallint", nullable: false),
                    BindType = table.Column<byte>(type: "smallint", nullable: false),
                    Upgrade = table.Column<byte>(type: "smallint", nullable: false),
                    Endurance = table.Column<byte>(type: "smallint", nullable: false),
                    SocketActive = table.Column<byte>(type: "smallint", nullable: false),
                    DueTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpgradeCount = table.Column<byte>(type: "smallint", nullable: false),
                    UpgradeLimit = table.Column<byte>(type: "smallint", nullable: false),
                    Flag = table.Column<byte>(type: "smallint", nullable: false),
                    Exp = table.Column<int>(type: "integer", nullable: false),
                    BroachState = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "000000000000000"),
                    RestoreCount = table.Column<byte>(type: "smallint", nullable: false),
                    SealCount = table.Column<byte>(type: "smallint", nullable: false),
                    OptionType = table.Column<short[]>(type: "smallint[]", nullable: false),
                    OptionValue = table.Column<int[]>(type: "integer[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_PersonId_Storage_Slot",
                table: "Items",
                columns: new[] { "PersonId", "Storage", "Slot" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropColumn(
                name: "BattlePoint",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Ether",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Gold",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "QuickSlotCards",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "QuickSlotItems",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "StorageGrades",
                table: "Persons");

            migrationBuilder.DropSequence(
                name: "ItemSerial");
        }
    }
}
