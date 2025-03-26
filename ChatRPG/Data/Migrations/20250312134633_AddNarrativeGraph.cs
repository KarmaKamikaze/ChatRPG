using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrativeGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NarrativeGraphId",
                table: "Campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NarrativeGraphs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeGraphs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NarrativeNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StoryContent = table.Column<string>(type: "text", nullable: false),
                    NodeStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NarrativeEdges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Conditions = table.Column<string[]>(type: "text[]", nullable: false),
                    TargetNodeId = table.Column<int>(type: "integer", nullable: false),
                    EdgeStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeEdges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NarrativeEdges_NarrativeNodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "NarrativeNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_NarrativeGraphId",
                table: "Campaigns",
                column: "NarrativeGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeEdges_TargetNodeId",
                table: "NarrativeEdges",
                column: "TargetNodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_NarrativeGraphs_NarrativeGraphId",
                table: "Campaigns",
                column: "NarrativeGraphId",
                principalTable: "NarrativeGraphs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_NarrativeGraphs_NarrativeGraphId",
                table: "Campaigns");

            migrationBuilder.DropTable(
                name: "NarrativeEdges");

            migrationBuilder.DropTable(
                name: "NarrativeGraphs");

            migrationBuilder.DropTable(
                name: "NarrativeNodes");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_NarrativeGraphId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "NarrativeGraphId",
                table: "Campaigns");
        }
    }
}
