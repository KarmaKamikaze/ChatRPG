namespace ChatRPG.Data.Models;

public class CharacterAbility
{
    public int CharacterId { get; set; }
    public int AbilityId { get; set; }
    public Character Character { get; set; } = null!;
    public Ability Ability { get; set; } = null!;
}
