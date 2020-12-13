using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class _12_12_20 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PortraitId",
                table: "characters",
                newName: "PhotoId");

            migrationBuilder.AddColumn<long>(
                name: "CharacterBackgroundId",
                table: "accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CharacterBackgroundId",
                table: "accounts");

            migrationBuilder.RenameColumn(
                name: "PhotoId",
                table: "characters",
                newName: "PortraitId");
        }
    }
}
