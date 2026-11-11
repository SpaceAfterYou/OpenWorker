using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ItemBindStatesAndTradePasswordTries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "TradePasswordFailed",
                table: "Accounts",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TradePasswordFailedAt",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: true);

            // The bind value of the items already stored was a copy of the rule of their table; it becomes their state.
            migrationBuilder.Sql(ItemBindStatesData.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TradePasswordFailed",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TradePasswordFailedAt",
                table: "Accounts");
        }
    }
}
