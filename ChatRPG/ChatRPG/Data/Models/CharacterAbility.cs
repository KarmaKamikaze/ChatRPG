namespace ChatRPG.Data.Models;

public class CharacterAbility
{
    private CharacterAbility() {}

    public CharacterAbility(Campaign campaign, Character character, Ability ability)
    {
        Campaign = campaign;
        Character = character;
        Ability = ability;
    }

    public int CharacterId { get; private set; }
    public int AbilityId { get; private set; }
    public Character Character { get; private set; } = null!;
    public Ability Ability { get; private set; } = null!;
    public Campaign Campaign { get; private set; } = null!;
}
