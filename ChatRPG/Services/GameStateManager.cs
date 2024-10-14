using ChatRPG.Data.Models;

namespace ChatRPG.Services;

public class GameStateManager(IPersistenceService persistenceService)
{
    public async Task SaveCurrentState(Campaign campaign)
    {
        await persistenceService.SaveAsync(campaign);
    }

    public static T ParseToEnum<T>(string input, T defaultVal) where T : struct, Enum
    {
        return Enum.TryParse(input, true, out T type) ? type : defaultVal;
    }
}
