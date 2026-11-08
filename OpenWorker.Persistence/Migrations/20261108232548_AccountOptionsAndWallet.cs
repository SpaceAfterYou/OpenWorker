using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <summary>
    /// The options of the client move from every person to the account, and a person keeps its whole wallet. Additive:
    /// the options of the persons stay where they were, and the account takes those of the person played last.
    /// </summary>
    public partial class AccountOptionsAndWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long[]>(
                name: "Currencies",
                table: "Persons",
                type: "bigint[]",
                nullable: false,
                defaultValue: new long[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "OptionList",
                table: "Accounts",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            // Every person of an account carried the same options, written together; the one played last wins if not.
            migrationBuilder.Sql("""
                UPDATE "Accounts" AS a
                SET "OptionList" = p."OptionList"
                FROM (
                    SELECT DISTINCT ON ("AccountId") "AccountId", "OptionList"
                    FROM "Persons"
                    WHERE length("OptionList") > 0
                    ORDER BY "AccountId", "LastSelectedAt" DESC NULLS LAST, "Id"
                ) AS p
                WHERE a."Id" = p."AccountId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currencies",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "OptionList",
                table: "Accounts");
        }
    }
}
