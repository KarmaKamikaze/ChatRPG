using System.Diagnostics.CodeAnalysis;

namespace ChatRPG.Data.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class Campaign
{
    private Campaign() {}

    public Campaign(User user, string title)
    {
        User = user;
        Title = title;
        StartedOn = DateTime.Now.ToUniversalTime();
    }

    public Campaign(User user, string title, StartScenario startScenario) : this(user, title)
    {
        StartScenario = startScenario;
    }

    public Campaign(User user, string title, string customStartScenario) : this(user, title)
    {
        CustomStartScenario = customStartScenario;
    }

    public int Id { get; private set; }
    public StartScenario? StartScenario { get; private set; }
    public User User { get; private set; } = null!;
    public string? CustomStartScenario { get; private set; }
    public string Title { get; private set; } = null!;
    public DateTime StartedOn { get; private set; }
}
