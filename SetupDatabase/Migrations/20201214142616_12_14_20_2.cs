using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class _12_14_20_2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SlotId",
                table: "characters",
                newName: "Slot");

            migrationBuilder.RenameColumn(
                name: "PhotoId",
                table: "characters",
                newName: "Photo");

            migrationBuilder.RenameColumn(
                name: "GeturesIds",
                table: "characters",
                newName: "Getures");

            migrationBuilder.RenameColumn(
                name: "GateId",
                table: "characters",
                newName: "Gate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Slot",
                table: "characters",
                newName: "SlotId");

            migrationBuilder.RenameColumn(
                name: "Photo",
                table: "characters",
                newName: "PhotoId");

            migrationBuilder.RenameColumn(
                name: "Getures",
                table: "characters",
                newName: "GeturesIds");

            migrationBuilder.RenameColumn(
                name: "Gate",
                table: "characters",
                newName: "GateId");
        }
    }
}
