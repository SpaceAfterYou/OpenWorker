using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SetupDatabase.Migrations
{
    public partial class _12_09_20_2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 6L);

            migrationBuilder.AlterColumn<int>(
                name: "CharacterId",
                table: "items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "guilds",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "AccountId",
                table: "characters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "characters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "CharacterId",
                table: "character_posts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "character_posts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "accounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "AccountId",
                table: "account_posts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "account_posts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "Id", "LastSelectedCharacterId", "Nickname", "Password", "SoulCash" },
                values: new object[,]
                {
                    { 1, -1, "sawich", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 2, -1, "Leeroy", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 3, -1, "Tweekly", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 4, -1, "Chelsea", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 5, -1, "Dez", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 6, -1, "Godo", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.AlterColumn<int>(
                name: "CharacterId",
                table: "items",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "items",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "guilds",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "AccountId",
                table: "characters",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "characters",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "CharacterId",
                table: "character_posts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "character_posts",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "accounts",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "AccountId",
                table: "account_posts",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "account_posts",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "Id", "LastSelectedCharacterId", "Nickname", "Password", "SoulCash" },
                values: new object[,]
                {
                    { 1L, -1, "sawich", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 2L, -1, "Leeroy", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 3L, -1, "Tweekly", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 4L, -1, "Chelsea", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 5L, -1, "Dez", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m },
                    { 6L, -1, "Godo", new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 }, 0m }
                });
        }
    }
}
