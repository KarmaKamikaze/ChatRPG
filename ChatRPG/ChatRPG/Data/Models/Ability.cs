namespace ChatRPG.Data.Models;

public class Ability
{
    private Ability() {}

    public Ability(string name, AbilityType type, int value)
    {
        Name = name;
        Type = type;
        Value = value;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public AbilityType Type { get; private set; } = AbilityType.Damage;
    public int Value { get; private set; }
    public ICollection<CharacterAbility> CharactersAbilities { get; } = new List<CharacterAbility>();
}
