using System.Diagnostics.CodeAnalysis;

namespace ChatRPG.Data.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class CharacterAbility
{
    private CharacterAbility()
    {
    }

    public CharacterAbility(Character character, Ability ability)
    {
        Character = character;
        Ability = ability;
    }

    public int CharacterId { get; private set; }
    public int AbilityId { get; private set; }
    public Character Character { get; private set; } = null!;
    public Ability Ability { get; private set; } = null!;
}
