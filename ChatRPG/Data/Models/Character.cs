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
            CharacterType.SmallMonster => 20,
            CharacterType.MediumMonster => 35,
            CharacterType.LargeMonster => 65,
            CharacterType.BossMonster => 90,
            _ => 50
        };
        if (isPlayer)
        {
            MaxHealth = 100;
        }

        CurrentHealth = MaxHealth;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public Environment Environment { get; set; } = null!;
    public CharacterType Type { get; private set; }
    public bool IsPlayer { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; set; } = null!;
    public byte[]? Portrait { get; set; }
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

    /// <summary>
    /// Deep copy of the character.
    /// </summary>
    /// <param name="campaign">The new campaign snapshot.</param>
    /// <param name="environment">The new environment copy.</param>
    /// <returns>A copy of the character.</returns>
    public Character DeepCopy(Campaign campaign, Environment environment)
    {
        return new Character(
            campaign,
            environment,
            Type,
            Name,
            Description,
            IsPlayer
        )
        {
            Portrait = Portrait != null ? (byte[])Portrait.Clone() : null,
            CurrentHealth = CurrentHealth
        };
    }
}
