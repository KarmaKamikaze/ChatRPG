namespace ChatRPG.Data.Models;

public class Character
{
    private Character()
    {
    }

    public Character(Campaign campaign, Environment environment, CharacterType type, string name, string description,
        bool isPlayer)
    {
        Campaign = campaign;
        Environment = environment;
        Type = type;
        Name = name;
        Description = description;
        IsPlayer = isPlayer;
        MaxHealth = type switch
        {
            CharacterType.Humanoid => 50,
            CharacterType.SmallCreature => 30,
            CharacterType.LargeCreature => 70,
            CharacterType.Monster => 90,
            _ => 50
        };
        if (isPlayer)
            MaxHealth = 100;
        CurrentHealth = MaxHealth;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public Environment Environment { get; set; } = null!;
    public CharacterType Type { get; private set; }
    public bool IsPlayer { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; set; } = null!;
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    /// <summary>
    /// Adjust the current health of this character.
    /// </summary>
    /// <param name="value">The value to adjust the current health with.</param>
    public bool AdjustHealth(int value)
    {
        CurrentHealth = Math.Min(MaxHealth, Math.Max(0, CurrentHealth + value));
        return CurrentHealth <= 0;
    }
}
