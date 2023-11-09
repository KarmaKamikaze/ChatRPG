namespace ChatRPG.Data.Models;

public class Character
{
    private Character()
    {
    }

    public Character(Campaign campaign, CharacterType type, string name, string description, bool isPlayer,
        int maxHealth)
    {
        Campaign = campaign;
        Type = type;
        Name = name;
        Description = description;
        IsPlayer = isPlayer;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public Character(Campaign campaign, CharacterType type, string name, string description, bool isPlayer,
        int maxHealth, int currentHealth)
        : this(campaign, type, name, description, isPlayer, maxHealth)
    {
        CurrentHealth = currentHealth;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public Environment Environment { get; set; } = null!;
    public CharacterType Type { get; private set; }
    public bool IsPlayer { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public ICollection<CharacterAbility?> CharacterAbilities { get; } = new List<CharacterAbility?>();

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

    /// <summary>
    /// Creates a <see cref="CharacterAbility"/> for this character and the given <paramref name="ability"/>, and adds it to its list of <see cref="CharacterAbilities"/> if it does not already exist.
    /// </summary>
    /// <param name="ability">The ability to add.</param>
    /// <returns>The created <see cref="CharacterAbility"/> entity.</returns>
    public CharacterAbility? AddAbility(Ability ability)
    {
        CharacterAbility? charAbility = CharacterAbilities.FirstOrDefault(a => a!.Ability == ability, null);
        if (charAbility is not null)
        {
            return charAbility;
        }
        charAbility = new CharacterAbility(this, ability);
        CharacterAbilities.Add(charAbility);

        return charAbility;
    }
}
