using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProloguePending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProloguePending",
                table: "Persons",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProloguePending",
                table: "Persons");
        }
    }
}
