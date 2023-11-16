using System.Collections.Concurrent;

namespace ChatRPG.Services;

public class CampaignMediatorService : ICampaignMediatorService
{
    public IDictionary<string, int> UserCampaignDict { get; set; } = new ConcurrentDictionary<string, int>();
}
