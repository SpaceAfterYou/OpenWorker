using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class _12_15_20_2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SlotId",
                table: "items",
                newName: "Slot");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Slot",
                table: "items",
                newName: "SlotId");
        }
    }
}
