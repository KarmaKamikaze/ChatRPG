using ChatRPG.Data.Models;

namespace ChatRPG.Services;

public class GameStateManager(IPersistenceService persistenceService)
{
    public async Task SaveCurrentState(Campaign campaign)
    {
        await persistenceService.SaveAsync(campaign);
    }
}
