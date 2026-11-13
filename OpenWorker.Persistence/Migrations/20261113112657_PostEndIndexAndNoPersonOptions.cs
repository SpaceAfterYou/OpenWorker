using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PostEndIndexAndNoPersonOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OptionList",
                table: "Persons");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ExpiresAt",
                table: "Posts",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_ExpiresAt",
                table: "Posts");

            migrationBuilder.AddColumn<byte[]>(
                name: "OptionList",
                table: "Persons",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
