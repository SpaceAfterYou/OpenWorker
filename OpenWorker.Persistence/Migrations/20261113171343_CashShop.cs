using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CashShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Cash",
                table: "Accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "CashShopPurchases",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Product = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShopPurchases", x => new { x.AccountId, x.PersonId, x.Product, x.Period });
                    table.CheckConstraint("CK_CashShopPurchases_Count", "\"Count\" > 0");
                    table.ForeignKey(
                        name: "FK_CashShopPurchases_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashShopSets",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    SetNo = table.Column<byte>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Products = table.Column<int[]>(type: "integer[]", nullable: false),
                    Items = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShopSets", x => new { x.PersonId, x.SetNo });
                    table.CheckConstraint("CK_CashShopSets_SetNo", "\"SetNo\" <= 8");
                    table.ForeignKey(
                        name: "FK_CashShopSets_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Accounts_Cash",
                table: "Accounts",
                sql: "\"Cash\" >= 0");

            // The cash shop is on for a new database; a row that is there already keeps its state.
            migrationBuilder.Sql("""
                INSERT INTO "ServerContents" ("Index", "State", "CreatedAt", "UpdatedAt")
                VALUES (4, TRUE, now(), now())
                ON CONFLICT ("Index") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashShopPurchases");

            migrationBuilder.DropTable(
                name: "CashShopSets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Accounts_Cash",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Cash",
                table: "Accounts");
        }
    }
}
