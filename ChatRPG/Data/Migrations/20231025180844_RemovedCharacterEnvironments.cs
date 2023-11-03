using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovedCharacterEnvironments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterEnvironments");

            migrationBuilder.AddColumn<int>(
                name: "EnvironmentId",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_EnvironmentId",
                table: "Characters",
                column: "EnvironmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Environments_EnvironmentId",
                table: "Characters",
                column: "EnvironmentId",
                principalTable: "Environments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Environments_EnvironmentId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_EnvironmentId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "Characters");

            migrationBuilder.CreateTable(
                name: "CharacterEnvironments",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    EnvironmentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEnvironments", x => new { x.CharacterId, x.Version });
                    table.ForeignKey(
                        name: "FK_CharacterEnvironments_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterEnvironments_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEnvironments_EnvironmentId",
                table: "CharacterEnvironments",
                column: "EnvironmentId");
        }
    }
}
