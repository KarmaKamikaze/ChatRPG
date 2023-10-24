namespace ChatRPG.Data.Models;

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
    public ICollection<CharacterAbility> CharacterAbilities { get; } = new List<CharacterAbility>();
    public ICollection<CharacterEnvironment> CharacterEnvironments { get; } = new List<CharacterEnvironment>();
    public Environment? CurrentEnvironment => CharacterEnvironments.MaxBy(x => x.Version)?.Environment;

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
    /// Creates a <see cref="CharacterEnvironment"/> for this character and the given <paramref name="environment"/>, and adds it to its list of <see cref="CharacterEnvironments"/>.
    /// </summary>
    /// <param name="environment">The environment to add.</param>
    /// <returns>The <see cref="CharacterEnvironment"/> that was created.</returns>
    public CharacterEnvironment AddEnvironment(Environment environment)
    {
        int version = 1;
        if (CharacterEnvironments.Any())
        {
            version = CharacterEnvironments.Max(c => c.Version) + 1;
        }
        var charEnv = new CharacterEnvironment(Campaign, this, environment, version);
        CharacterEnvironments.Add(charEnv);
        return charEnv;
    }

    /// <summary>
    /// Creates a <see cref="CharacterAbility"/> for this character and the given <paramref name="ability"/>, and adds it to its list of <see cref="CharacterAbilities"/> if it does not already exist.
    /// </summary>
    /// <param name="ability">The ability to add.</param>
    /// <returns>The created <see cref="CharacterAbility"/> entity.</returns>
    public CharacterAbility AddAbility(Ability ability)
    {
        var charAbility = CharacterAbilities.FirstOrDefault(a => a!.Ability == ability, null);
        if (charAbility is not null)
        {
            return charAbility;
        }
        charAbility = new CharacterAbility(Campaign, this, ability);
        CharacterAbilities.Add(charAbility);

        return charAbility;
    }
}
