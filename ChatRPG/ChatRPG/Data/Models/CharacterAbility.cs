namespace ChatRPG.Data.Models;

public class CharacterAbility
{
    private CharacterAbility() {}

    public CharacterAbility(Character character, Ability ability)
    {
        Character = character;
        Ability = ability;
    }

    private int CharacterId { get; set; }
    private int AbilityId { get; set; }
    public Character Character { get; private set; } = null!;
    public Ability Ability { get; private set; } = null!;
}
