namespace ChatRPG.Data.Models;

public class Modifier
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public ModifierType Type { get; set; } = ModifierType.Armor;
    public int Value { get; set; }
}
