namespace ChatRPG.Data.Models;

public class Ability
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public AbilityType Type { get; set; } = AbilityType.Damage;
    public int Value { get; set; } = 0;
    public int AbilityPointCost { get; set; } = 1;
}
