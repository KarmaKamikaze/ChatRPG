using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVerdicts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Message_Campaigns_CampaignId",
                table: "Message");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Message",
                table: "Message");

            migrationBuilder.RenameTable(
                name: "Message",
                newName: "Messages");

            migrationBuilder.RenameIndex(
                name: "IX_Message_CampaignId",
                table: "Messages",
                newName: "IX_Messages_CampaignId");

            migrationBuilder.AddColumn<int>(
                name: "VerdictId",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Verdicts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verdicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Verdicts_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_VerdictId",
                table: "Messages",
                column: "VerdictId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Verdicts_CampaignId",
                table: "Verdicts",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Campaigns_CampaignId",
                table: "Messages",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Verdicts_VerdictId",
                table: "Messages",
                column: "VerdictId",
                principalTable: "Verdicts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Campaigns_CampaignId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Verdicts_VerdictId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "Verdicts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_VerdictId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "VerdictId",
                table: "Messages");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "Message");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_CampaignId",
                table: "Message",
                newName: "IX_Message_CampaignId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Message",
                table: "Message",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Message_Campaigns_CampaignId",
                table: "Message",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
