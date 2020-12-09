using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class _12_8_20 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_accounts_SessionKey",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "Mac",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "SessionKey",
                table: "accounts");

            migrationBuilder.RenameColumn(
                name: "LastSelectedCharacter",
                table: "accounts",
                newName: "LastSelectedCharacterId");

            migrationBuilder.AlterColumn<byte>(
                name: "Hero",
                table: "characters",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastSelectedCharacterId",
                table: "accounts",
                newName: "LastSelectedCharacter");

            migrationBuilder.AlterColumn<int>(
                name: "Hero",
                table: "characters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.AddColumn<string>(
                name: "Mac",
                table: "accounts",
                type: "CHAR(18)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SessionKey",
                table: "accounts",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 1L,
                column: "Mac",
                value: "00-00-00-00-00-00");

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 2L,
                column: "Mac",
                value: "00-00-00-00-00-00");

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 3L,
                column: "Mac",
                value: "00-00-00-00-00-00");

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 4L,
                column: "Mac",
                value: "00-00-00-00-00-00");

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 5L,
                column: "Mac",
                value: "00-00-00-00-00-00");

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 6L,
                column: "Mac",
                value: "00-00-00-00-00-00");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_SessionKey",
                table: "accounts",
                column: "SessionKey");
        }
    }
}