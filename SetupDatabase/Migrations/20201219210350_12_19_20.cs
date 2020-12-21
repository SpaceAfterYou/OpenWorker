using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class _12_19_20 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "characters",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "characters",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(12)",
                oldMaxLength: 12);
        }
    }
}
