namespace ChatRPG.Data.Models;

public class CharacterEnvironment
{
    private CharacterEnvironment() {}

    public CharacterEnvironment(Character character, Environment environment, int version)
    {
        Character = character;
        Environment = environment;
        Version = version;
    }

    private int CharacterId { get; set; }
    public Character Character { get; private set; } = null!;
    public int Version { get; private set; }
    public Environment Environment { get; private set; } = null!;
}
