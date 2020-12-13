using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class _12_12_20_2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastSelectedCharacterId",
                table: "accounts",
                newName: "LastSelectedCharacter");

            migrationBuilder.RenameColumn(
                name: "FavoriteCharacterId",
                table: "accounts",
                newName: "FavoriteCharacter");

            migrationBuilder.RenameColumn(
                name: "CharacterBackgroundId",
                table: "accounts",
                newName: "CharacterBackground");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastSelectedCharacter",
                table: "accounts",
                newName: "LastSelectedCharacterId");

            migrationBuilder.RenameColumn(
                name: "FavoriteCharacter",
                table: "accounts",
                newName: "FavoriteCharacterId");

            migrationBuilder.RenameColumn(
                name: "CharacterBackground",
                table: "accounts",
                newName: "CharacterBackgroundId");
        }
    }
}
