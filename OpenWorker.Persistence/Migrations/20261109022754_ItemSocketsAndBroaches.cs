using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ItemSocketsAndBroaches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "Broaches",
                table: "Items",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<short[]>(
                name: "SocketOptionType",
                table: "Items",
                type: "smallint[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int[]>(
                name: "SocketOptionValue",
                table: "Items",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int[]>(
                name: "Sockets",
                table: "Items",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Broaches",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SocketOptionType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SocketOptionValue",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Sockets",
                table: "Items");
        }
    }
}
