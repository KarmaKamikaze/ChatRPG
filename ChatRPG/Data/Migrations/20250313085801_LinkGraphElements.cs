using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkGraphElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NarrativeEdges_NarrativeNodes_TargetNodeId",
                table: "NarrativeEdges");

            migrationBuilder.AddColumn<int>(
                name: "GraphId",
                table: "NarrativeNodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SourceNodeId",
                table: "NarrativeEdges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeNodes_GraphId",
                table: "NarrativeNodes",
                column: "GraphId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeEdges_SourceNodeId",
                table: "NarrativeEdges",
                column: "SourceNodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_NarrativeEdges_NarrativeNodes_SourceNodeId",
                table: "NarrativeEdges",
                column: "SourceNodeId",
                principalTable: "NarrativeNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NarrativeEdges_NarrativeNodes_TargetNodeId",
                table: "NarrativeEdges",
                column: "TargetNodeId",
                principalTable: "NarrativeNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NarrativeNodes_NarrativeGraphs_GraphId",
                table: "NarrativeNodes",
                column: "GraphId",
                principalTable: "NarrativeGraphs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NarrativeEdges_NarrativeNodes_SourceNodeId",
                table: "NarrativeEdges");

            migrationBuilder.DropForeignKey(
                name: "FK_NarrativeEdges_NarrativeNodes_TargetNodeId",
                table: "NarrativeEdges");

            migrationBuilder.DropForeignKey(
                name: "FK_NarrativeNodes_NarrativeGraphs_GraphId",
                table: "NarrativeNodes");

            migrationBuilder.DropIndex(
                name: "IX_NarrativeNodes_GraphId",
                table: "NarrativeNodes");

            migrationBuilder.DropIndex(
                name: "IX_NarrativeEdges_SourceNodeId",
                table: "NarrativeEdges");

            migrationBuilder.DropColumn(
                name: "GraphId",
                table: "NarrativeNodes");

            migrationBuilder.DropColumn(
                name: "SourceNodeId",
                table: "NarrativeEdges");

            migrationBuilder.AddForeignKey(
                name: "FK_NarrativeEdges_NarrativeNodes_TargetNodeId",
                table: "NarrativeEdges",
                column: "TargetNodeId",
                principalTable: "NarrativeNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
