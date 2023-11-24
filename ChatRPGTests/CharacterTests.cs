using ChatRPG.Data.Models;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPGTests;

public class CharacterTests
{
    [Theory]
    [InlineData(-200, 0)]
    [InlineData(-20, 80)]
    [InlineData(-100, 0)]
    [InlineData(10, 100)]
    [InlineData(-5, 95)]
    public void AdjustHealth_VariousInput_StaysWithinValidRange(int adjustByAmount, int expectedCurrentHealth)
    {
        Campaign campaign = new Campaign(new User(), "");
        Character character = new Character(campaign, new Environment(campaign, "", ""), CharacterType.Humanoid, "", "",
            true);

        character.AdjustHealth(adjustByAmount);

        Assert.Equal(expectedCurrentHealth, character.CurrentHealth);
    }
}
