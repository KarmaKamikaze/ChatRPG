using System.Diagnostics.CodeAnalysis;

namespace ChatRPG.Data.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class Ability
{
    private Ability()
    {
    }

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
}
