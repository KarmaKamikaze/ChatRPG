using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRPG.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedPresetStartScenarios : Migration
    {
        private readonly List<Tuple<string, string>> _tuples = new()
        {
            new Tuple<string, string>("The Forgotten Artifacts of Aldorath",
                "The peaceful kingdom of Aldorath is now facing imminent dangers from a prophecy that foretells its " +
                "downfall. The only hope lies in the legend of seven ancient artifacts rumored to have immense power. " +
                "The player starts as an inexperienced adventurer summoned by the king."
            ),
            new Tuple<string, string>("Shadows over Silverpine",
                "In the remote, usually tranquil village of Silverpine, people have been mysteriously vanishing " +
                "during the night. The player starts as a lone investigator hired by the desperate villagers " +
                "to discover the source of these unsettling circumstances."
            ),
            new Tuple<string, string>("The Ashen Veil",
                "A deadly, visibly moving fog known as the Ashen Veil is swallowing cities one by one. The player " +
                "begins as an adventurer in a small town on the edge of the fog''s destructive " +
                "path, tasked with finding a way to halt the approaching doom."
            ),
            new Tuple<string, string>("Whispers of the Void",
                "An ancient, long-forgotten deity has been awakened, threating to engulf the world into chaos and " +
                "darkness. The player is chosen by an opposing deity and begin their journey at a secluded temple, " +
                "receiving their divine mission from the temple''s oracle."
            ),
            new Tuple<string, string>("The Lost Kingdom of Zur",
                "The legendary submerged city of Zur, believed to be a myth, has risen from the depths of the ocean, " +
                "bringing about seismic disturbances around the world and causing sea creatures to behave abnormally. " +
                "The player starts their adventure as part of a commissioned exploration team, tasked by the Grand " +
                "Council to investigate this unprecedented event."
            )
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _tuples.ForEach(t => migrationBuilder.Sql($"insert into \"StartScenarios\" (\"Title\", \"Body\") values ('{t.Item1}','{t.Item2}');"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"delete from \"StartScenarios\" where \"Title\" in ({string.Join(',', _tuples.Select(t => $"'{t.Item1}'"))});");
        }
    }
}
