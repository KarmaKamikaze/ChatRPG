namespace ChatRPG.Data.Models;

public class Campaign
{
    private Campaign()
    {
    }

    public Campaign(User user, string title)
    {
        User = user;
        Title = title;
        StartedOn = DateTime.UtcNow;
    }

    public Campaign(User user, string title, string startScenario) : this(user, title)
    {
        StartScenario = startScenario;
    }

    public int Id { get; private set; }
    public string? StartScenario { get; private set; }
    public User User { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public DateTime StartedOn { get; private set; }
    public ICollection<Message> Messages { get; } = new List<Message>();
    public ICollection<Character> Characters { get; } = new List<Character>();
    public ICollection<Event> Events { get; } = new List<Event>();
    public ICollection<Environment> Environments { get; } = new List<Environment>();
    public Character Player => Characters.First(c => c.IsPlayer);
}
