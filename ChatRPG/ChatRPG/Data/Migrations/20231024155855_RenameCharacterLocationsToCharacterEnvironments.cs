using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCharacterLocationsToCharacterEnvironments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterLocations_Campaigns_CampaignId",
                table: "CharacterLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterLocations_Characters_CharacterId",
                table: "CharacterLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterLocations_Environments_EnvironmentId",
                table: "CharacterLocations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CharacterLocations",
                table: "CharacterLocations");

            migrationBuilder.RenameTable(
                name: "CharacterLocations",
                newName: "CharacterEnvironments");

            migrationBuilder.RenameIndex(
                name: "IX_CharacterLocations_EnvironmentId",
                table: "CharacterEnvironments",
                newName: "IX_CharacterEnvironments_EnvironmentId");

            migrationBuilder.RenameIndex(
                name: "IX_CharacterLocations_CampaignId",
                table: "CharacterEnvironments",
                newName: "IX_CharacterEnvironments_CampaignId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CharacterEnvironments",
                table: "CharacterEnvironments",
                columns: new[] { "CharacterId", "Version" });

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterEnvironments_Campaigns_CampaignId",
                table: "CharacterEnvironments",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterEnvironments_Characters_CharacterId",
                table: "CharacterEnvironments",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterEnvironments_Environments_EnvironmentId",
                table: "CharacterEnvironments",
                column: "EnvironmentId",
                principalTable: "Environments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);


            migrationBuilder.Sql(@"drop trigger if exists ""OnCharacterLocationsInsert"" on ""CharacterLocations"" cascade;");
            migrationBuilder.Sql(@"drop function if exists ""UpdateCharacterLocationsVersion"" cascade;");

            migrationBuilder.Sql(@"
                create or replace function ""UpdateCharacterEnvironmentsVersion""()
                    returns trigger
                    language plpgsql
                as $$
                    declare maxVersion integer;
                    begin
                        select coalesce(max(""Version""), 0) from ""CharacterEnvironments""
                        where ""CharacterId"" = NEW.""CharacterId""
                        into maxVersion;

                        NEW.""Version"" = maxVersion + 1;
                        return NEW;
                    end;
                $$;
            ");

            migrationBuilder.Sql(@"
                create or replace trigger ""OnCharacterEnvironmentsInsert""
                    before insert on ""CharacterEnvironments""
                    for each row execute procedure ""UpdateCharacterEnvironmentsVersion""();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterEnvironments_Campaigns_CampaignId",
                table: "CharacterEnvironments");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterEnvironments_Characters_CharacterId",
                table: "CharacterEnvironments");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterEnvironments_Environments_EnvironmentId",
                table: "CharacterEnvironments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CharacterEnvironments",
                table: "CharacterEnvironments");

            migrationBuilder.RenameTable(
                name: "CharacterEnvironments",
                newName: "CharacterLocations");

            migrationBuilder.RenameIndex(
                name: "IX_CharacterEnvironments_EnvironmentId",
                table: "CharacterLocations",
                newName: "IX_CharacterLocations_EnvironmentId");

            migrationBuilder.RenameIndex(
                name: "IX_CharacterEnvironments_CampaignId",
                table: "CharacterLocations",
                newName: "IX_CharacterLocations_CampaignId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CharacterLocations",
                table: "CharacterLocations",
                columns: new[] { "CharacterId", "Version" });

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterLocations_Campaigns_CampaignId",
                table: "CharacterLocations",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterLocations_Characters_CharacterId",
                table: "CharacterLocations",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterLocations_Environments_EnvironmentId",
                table: "CharacterLocations",
                column: "EnvironmentId",
                principalTable: "Environments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(@"drop trigger if exists ""OnCharacterEnvironmentsInsert"" on ""CharacterEnvironments"" cascade;");
            migrationBuilder.Sql(@"drop function if exists ""UpdateCharacterEnvironmentsVersion"" cascade;");

            migrationBuilder.Sql(@"
                create or replace function ""UpdateCharacterLocationsVersion""()
                    returns trigger
                    language plpgsql
                as $$
                    declare maxVersion integer;
                    begin
                        select coalesce(max(""Version""), 0) from ""CharacterLocations""
                        where ""CharacterId"" = NEW.""CharacterId""
                        into maxVersion;

                        NEW.""Version"" = maxVersion + 1;
                        return NEW;
                    end;
                $$;
            ");

            migrationBuilder.Sql(@"
                create or replace trigger ""OnCharacterLocationsInsert""
                    before insert on ""CharacterLocations""
                    for each row execute procedure ""UpdateCharacterLocationsVersion""();
            ");
        }
    }
}
