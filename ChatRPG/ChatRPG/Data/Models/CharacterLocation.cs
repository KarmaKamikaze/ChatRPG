namespace ChatRPG.Data.Models;

public class CharacterLocation
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public int Version { get; set; } = 1;
    public Environment Environment { get; set; } = null!;
}
