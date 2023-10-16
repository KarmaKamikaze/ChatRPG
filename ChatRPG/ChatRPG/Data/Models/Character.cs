using System.Diagnostics.CodeAnalysis;

namespace ChatRPG.Data.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
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
        _currentHealth = maxHealth;
    }

    public Character(Campaign campaign, CharacterType type, string name, string description, bool isPlayer, int maxHealth, int currentHealth)
        : this(campaign, type, name, description, isPlayer, maxHealth)
    {
        _currentHealth = currentHealth;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public CharacterType Type { get; private set; }
    public bool IsPlayer { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int MaxHealth { get; private set; }

    private int _currentHealth;
    public int CurrentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = Math.Min(value, MaxHealth);
            if (_currentHealth == 0)
            {
                // TODO: trigger character death event
            }
        }
    }
}
