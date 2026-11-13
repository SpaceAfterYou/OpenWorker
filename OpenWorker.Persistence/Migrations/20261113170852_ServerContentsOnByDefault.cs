using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ServerContentsOnByDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Attendance and the ranking are on for a new database; a row that is there already keeps its state.
            migrationBuilder.Sql("""
                INSERT INTO "ServerContents" ("Index", "State", "CreatedAt", "UpdatedAt")
                VALUES (0, TRUE, now(), now()), (3, TRUE, now(), now())
                ON CONFLICT ("Index") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
