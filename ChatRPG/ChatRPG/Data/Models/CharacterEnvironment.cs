namespace ChatRPG.Data.Models;

public class CharacterEnvironment
{
    private CharacterEnvironment() {}

    public CharacterEnvironment(Campaign campaign, Character character, Environment environment, int version)
    {
        Campaign = campaign;
        Character = character;
        Environment = environment;
        Version = version;
    }

    public int CharacterId { get; private set; }
    public Character Character { get; private set; } = null!;
    public int Version { get; private set; }
    public Environment Environment { get; private set; } = null!;
    public Campaign Campaign { get; private set; } = null!;
}
