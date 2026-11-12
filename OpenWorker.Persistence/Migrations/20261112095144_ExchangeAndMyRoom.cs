using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExchangeAndMyRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeInterests",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Item = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeInterests", x => new { x.PersonId, x.Item });
                    table.ForeignKey(
                        name: "FK_ExchangeInterests_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Serial = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"ItemSerial\"')"),
                    SellerId = table.Column<int>(type: "integer", nullable: false),
                    SellerName = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    SellerHero = table.Column<byte>(type: "smallint", nullable: false),
                    Item = table.Column<int>(type: "integer", nullable: false),
                    InitialCount = table.Column<short>(type: "smallint", nullable: false),
                    Count = table.Column<short>(type: "smallint", nullable: false),
                    State = table.Column<byte>(type: "smallint", nullable: false),
                    PriceOne = table.Column<long>(type: "bigint", nullable: false),
                    SaleRate = table.Column<byte>(type: "smallint", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Group = table.Column<byte>(type: "smallint", nullable: false),
                    SubGroup = table.Column<byte>(type: "smallint", nullable: false),
                    UseClass = table.Column<byte>(type: "smallint", nullable: false),
                    Level = table.Column<short>(type: "smallint", nullable: false),
                    Rank = table.Column<byte>(type: "smallint", nullable: false),
                    Upgrade = table.Column<byte>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Goods = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeListings", x => x.Id);
                    table.CheckConstraint("CK_ExchangeListings_Count", "\"Count\" >= 0 AND \"Count\" <= \"InitialCount\"");
                    table.CheckConstraint("CK_ExchangeListings_OnSale", "\"State\" <> 2 OR (\"PriceOne\" > 0 AND \"ExpiresAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_ExchangeListings_Price", "\"PriceOne\" >= 0");
                    table.CheckConstraint("CK_ExchangeListings_SaleRate", "\"SaleRate\" <= 100");
                    table.ForeignKey(
                        name: "FK_ExchangeListings_Persons_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeSales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Item = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<short>(type: "smallint", nullable: false),
                    PriceOne = table.Column<long>(type: "bigint", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeSales", x => x.Id);
                    table.CheckConstraint("CK_ExchangeSales_Count", "\"Count\" > 0");
                    table.CheckConstraint("CK_ExchangeSales_Price", "\"PriceOne\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "MyRooms",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    GateId = table.Column<short>(type: "smallint", nullable: false),
                    Index = table.Column<short>(type: "smallint", nullable: false),
                    Grid = table.Column<short>(type: "smallint", nullable: false),
                    OpenLevel = table.Column<byte>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyRooms", x => new { x.AccountId, x.GateId });
                    table.ForeignKey(
                        name: "FK_MyRooms_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MyRooms_Gates_GateId",
                        column: x => x.GateId,
                        principalTable: "Gates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MyRoomFurniture",
                columns: table => new
                {
                    Serial = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    GateId = table.Column<short>(type: "smallint", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Rotation = table.Column<byte>(type: "smallint", nullable: false),
                    Grid = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyRoomFurniture", x => x.Serial);
                    table.ForeignKey(
                        name: "FK_MyRoomFurniture_MyRooms_AccountId_GateId",
                        columns: x => new { x.AccountId, x.GateId },
                        principalTable: "MyRooms",
                        principalColumns: new[] { "AccountId", "GateId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeListings_SellerId_State",
                table: "ExchangeListings",
                columns: new[] { "SellerId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeListings_Serial",
                table: "ExchangeListings",
                column: "Serial",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeListings_State_Group_SubGroup_PriceOne",
                table: "ExchangeListings",
                columns: new[] { "State", "Group", "SubGroup", "PriceOne" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeListings_State_Item_PriceOne",
                table: "ExchangeListings",
                columns: new[] { "State", "Item", "PriceOne" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeSales_Item_SoldAt",
                table: "ExchangeSales",
                columns: new[] { "Item", "SoldAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MyRoomFurniture_AccountId_GateId",
                table: "MyRoomFurniture",
                columns: new[] { "AccountId", "GateId" });

            migrationBuilder.CreateIndex(
                name: "IX_MyRooms_GateId",
                table: "MyRooms",
                column: "GateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeInterests");

            migrationBuilder.DropTable(
                name: "ExchangeListings");

            migrationBuilder.DropTable(
                name: "ExchangeSales");

            migrationBuilder.DropTable(
                name: "MyRoomFurniture");

            migrationBuilder.DropTable(
                name: "MyRooms");
        }
    }
}
