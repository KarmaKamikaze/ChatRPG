using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CombatMode",
                table: "Campaigns");

            migrationBuilder.AddColumn<string>(
                name: "GameSummary",
                table: "Campaigns",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameSummary",
                table: "Campaigns");

            migrationBuilder.AddColumn<bool>(
                name: "CombatMode",
                table: "Campaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
