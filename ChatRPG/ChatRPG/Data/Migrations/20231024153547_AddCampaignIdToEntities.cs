using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignIdToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Ordering",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AbilityId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnvironmentId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "CharacterLocations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "CharacterAbilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "Abilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_AbilityId",
                table: "Characters",
                column: "AbilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_EnvironmentId",
                table: "Characters",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterLocations_CampaignId",
                table: "CharacterLocations",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAbilities_CampaignId",
                table: "CharacterAbilities",
                column: "CampaignId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterAbilities_Campaigns_CampaignId",
                table: "CharacterAbilities",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterLocations_Campaigns_CampaignId",
                table: "CharacterLocations",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Abilities_AbilityId",
                table: "Characters",
                column: "AbilityId",
                principalTable: "Abilities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Environments_EnvironmentId",
                table: "Characters",
                column: "EnvironmentId",
                principalTable: "Environments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Abilities_Campaigns_CampaignId",
                table: "Abilities");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterAbilities_Campaigns_CampaignId",
                table: "CharacterAbilities");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterLocations_Campaigns_CampaignId",
                table: "CharacterLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Abilities_AbilityId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Environments_EnvironmentId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_AbilityId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_EnvironmentId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_CharacterLocations_CampaignId",
                table: "CharacterLocations");

            migrationBuilder.DropIndex(
                name: "IX_CharacterAbilities_CampaignId",
                table: "CharacterAbilities");

            migrationBuilder.DropIndex(
                name: "IX_Abilities_CampaignId",
                table: "Abilities");

            migrationBuilder.DropColumn(
                name: "AbilityId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "CharacterLocations");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "CharacterAbilities");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Abilities");

            migrationBuilder.AlterColumn<int>(
                name: "Ordering",
                table: "Events",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);
        }
    }
}
