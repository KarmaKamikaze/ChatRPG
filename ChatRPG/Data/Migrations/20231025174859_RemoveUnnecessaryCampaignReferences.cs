using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnnecessaryCampaignReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterAbilities_Campaigns_CampaignId",
                table: "CharacterAbilities");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterEnvironments_Campaigns_CampaignId",
                table: "CharacterEnvironments");

            migrationBuilder.DropIndex(
                name: "IX_CharacterEnvironments_CampaignId",
                table: "CharacterEnvironments");

            migrationBuilder.DropIndex(
                name: "IX_CharacterAbilities_CampaignId",
                table: "CharacterAbilities");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "CharacterEnvironments");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "CharacterAbilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "CharacterEnvironments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "CharacterAbilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEnvironments_CampaignId",
                table: "CharacterEnvironments",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAbilities_CampaignId",
                table: "CharacterAbilities",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterAbilities_Campaigns_CampaignId",
                table: "CharacterAbilities",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterEnvironments_Campaigns_CampaignId",
                table: "CharacterEnvironments",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
