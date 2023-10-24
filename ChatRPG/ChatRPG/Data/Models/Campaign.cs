namespace ChatRPG.Data.Models;

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
    public ICollection<Character> Characters { get; } = new List<Character>();
    public ICollection<Event> Events { get; } = new List<Event>();
    public ICollection<Environment> Environments { get; } = new List<Environment>();
    public ICollection<Ability> Abilities { get; } = new List<Ability>();
}
