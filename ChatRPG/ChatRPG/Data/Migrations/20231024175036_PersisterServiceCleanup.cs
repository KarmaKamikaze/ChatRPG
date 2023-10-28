using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersisterServiceCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "AbilityId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "Characters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_Characters_AbilityId",
                table: "Characters",
                column: "AbilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_EnvironmentId",
                table: "Characters",
                column: "EnvironmentId");

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
    }
}
