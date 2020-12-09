using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SetupDatabase.Migrations
{
    public partial class _12_09_20_5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long[]>(
                name: "GeturesIds",
                table: "characters",
                type: "bigint[]",
                nullable: false,
                defaultValue: new long[0]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeturesIds",
                table: "characters");
        }
    }
}
