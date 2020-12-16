using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class _12_16_20 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Getures",
                table: "characters",
                newName: "Gestures");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Gestures",
                table: "characters",
                newName: "Getures");
        }
    }
}
