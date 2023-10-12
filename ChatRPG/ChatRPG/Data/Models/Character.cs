namespace ChatRPG.Data.Models;

public class Character
{
    public int Id { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public CharacterType Type { get; set; } = CharacterType.Humanoid;
    public bool IsPlayer { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxAbilityPoints { get; set; }
    public int CurrentAbilityPoints { get; set; }
    public int Strength { get; set; } = 1;
    public int Dexterity { get; set; } = 1;
    public int Constitution { get; set; } = 1;
    public int Intelligence { get; set; } = 1;
    public int Currency { get; set; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
}
