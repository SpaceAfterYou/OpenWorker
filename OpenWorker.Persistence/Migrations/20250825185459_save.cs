using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class save : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TitleSecondary",
                table: "Persons",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "TitlePrimary",
                table: "Persons",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int[]>(
                name: "GestureList",
                table: "Persons",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "OptionList",
                table: "Persons",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "PersonPersistentPersonPersistent",
                columns: table => new
                {
                    BlockedListId = table.Column<int>(type: "integer", nullable: false),
                    FriendListId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonPersistentPersonPersistent", x => new { x.BlockedListId, x.FriendListId });
                    table.ForeignKey(
                        name: "FK_PersonPersistentPersonPersistent_Persons_BlockedListId",
                        column: x => x.BlockedListId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonPersistentPersonPersistent_Persons_FriendListId",
                        column: x => x.FriendListId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonPersistentPersonPersistent_FriendListId",
                table: "PersonPersistentPersonPersistent",
                column: "FriendListId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonPersistentPersonPersistent");

            migrationBuilder.DropColumn(
                name: "GestureList",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "OptionList",
                table: "Persons");

            migrationBuilder.AlterColumn<long>(
                name: "TitleSecondary",
                table: "Persons",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "TitlePrimary",
                table: "Persons",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
