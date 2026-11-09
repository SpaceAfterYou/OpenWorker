using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Posts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: true),
                    SenderName = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    SenderHero = table.Column<byte>(type: "smallint", nullable: false),
                    ReceiverId = table.Column<int>(type: "integer", nullable: true),
                    ReceiverName = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    ReceiverHero = table.Column<byte>(type: "smallint", nullable: false),
                    ReceiverAccountId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Body = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Money = table.Column<long>(type: "bigint", nullable: false),
                    Postage = table.Column<long>(type: "bigint", nullable: false),
                    MainType = table.Column<byte>(type: "smallint", nullable: false),
                    SubType = table.Column<byte>(type: "smallint", nullable: false),
                    State = table.Column<byte>(type: "smallint", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Items = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.CheckConstraint("CK_Posts_Money", "\"Money\" >= 0");
                    table.CheckConstraint("CK_Posts_Postage", "\"Postage\" >= 0");
                    table.CheckConstraint("CK_Posts_Receiver", "(\"Kind\" = 0 AND \"ReceiverId\" IS NOT NULL AND \"ReceiverAccountId\" IS NULL) OR (\"Kind\" = 1 AND \"ReceiverAccountId\" IS NOT NULL AND \"ReceiverId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_Posts_Accounts_ReceiverAccountId",
                        column: x => x.ReceiverAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Posts_Persons_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Posts_Persons_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ReceiverAccountId",
                table: "Posts",
                column: "ReceiverAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ReceiverId",
                table: "Posts",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SenderId",
                table: "Posts",
                column: "SenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Posts");
        }
    }
}
