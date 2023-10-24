using System.Diagnostics.CodeAnalysis;

namespace ChatRPG.Data.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class CharacterLocation
{
    private CharacterLocation()
    {
    }

    public CharacterLocation(Character character, Environment environment)
    {
        Character = character;
        Environment = environment;
    }

    public int CharacterId { get; private set; }
    public Character Character { get; private set; } = null!;
    public int Version { get; private set; }
    public Environment Environment { get; private set; } = null!;
}
