using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonTitlesAndCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommunityComment",
                table: "Persons",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CommunityMemo",
                table: "Persons",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "CommunityState",
                table: "Persons",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "TitleAbilityPrimary",
                table: "Persons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TitleAbilitySecondary",
                table: "Persons",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommunityComment",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "CommunityMemo",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "CommunityState",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "TitleAbilityPrimary",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "TitleAbilitySecondary",
                table: "Persons");
        }
    }
}
