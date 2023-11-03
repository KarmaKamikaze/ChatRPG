using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEvenMoreUnnecessaryCampaignReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Abilities_Campaigns_CampaignId",
                table: "Abilities");

            migrationBuilder.DropIndex(
                name: "IX_Abilities_CampaignId",
                table: "Abilities");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Abilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "Abilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Abilities_CampaignId",
                table: "Abilities",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Abilities_Campaigns_CampaignId",
                table: "Abilities",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
