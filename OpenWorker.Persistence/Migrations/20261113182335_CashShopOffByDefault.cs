using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CashShopOffByDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The window of the cash shop is not proven in the client yet: off until it is.
            migrationBuilder.Sql("""
                UPDATE "ServerContents" SET "State" = FALSE, "UpdatedAt" = now() WHERE "Index" = 4;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
