using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CashShopOnByDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The shop's window opens in the client through the menu: on by default.
            migrationBuilder.Sql("""
                UPDATE "ServerContents" SET "State" = TRUE, "UpdatedAt" = now() WHERE "Index" = 4;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
