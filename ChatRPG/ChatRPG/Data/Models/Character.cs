using System.Diagnostics.CodeAnalysis;

namespace ChatRPG.Data.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Character
{
    private Character() {}

    public Character(Campaign campaign, CharacterType type, string name, string description, bool isPlayer, int maxHealth)
    {
        Campaign = campaign;
        Type = type;
        Name = name;
        Description = description;
        IsPlayer = isPlayer;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public Character(Campaign campaign, CharacterType type, string name, string description, bool isPlayer, int maxHealth, int currentHealth)
        : this(campaign, type, name, description, isPlayer, maxHealth)
    {
        CurrentHealth = currentHealth;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public CharacterType Type { get; private set; }
    public bool IsPlayer { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int MaxHealth { get; private set; }

    public int CurrentHealth { get; private set; }

    /// <summary>
    /// Adjust the current health of this character.
    /// </summary>
    /// <param name="value">The value to adjust the current health with.</param>
    public void AdjustHealth(int value)
    {
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + value);
        if (CurrentHealth <= 0)
        {
            // TODO: Handle character death
        }
    }
}
