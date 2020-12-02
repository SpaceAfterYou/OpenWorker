using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Migration.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    Id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nickname = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    LastSelectedCharacter = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    SessionKey = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    Password = table.Column<byte[]>(type: "varbinary(64)", maxLength: 64, nullable: false),
                    Mac = table.Column<string>(type: "varchar(18)", maxLength: 18, nullable: false, defaultValue: "00-00-00-00-00-00"),
                    SoulCash = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => new { x.Id, x.Nickname });
                });

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "Id", "Nickname", "Password" },
                values: new object[,]
                {
                    { 1u, "sawich", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 } },
                    { 2u, "Leeroy", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 } },
                    { 3u, "Tweekly", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 } },
                    { 4u, "Chelsea", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 } },
                    { 5u, "Dez", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 } },
                    { 6u, "Godo", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 } }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
