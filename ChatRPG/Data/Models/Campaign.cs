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

    public Campaign(User user, string title, string startScenario, bool isOpenWorld) : this(user, title)
    {
        StartScenario = startScenario;
        IsOpenWorld = isOpenWorld;
    }

    public int Id { get; private set; }
    public string? StartScenario { get; set; }
    public User User { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public DateTime StartedOn { get; private set; }
    public ICollection<Message> Messages { get; } = new List<Message>();
    public string GameSummary { get; set; } = string.Empty;
    public ICollection<Character> Characters { get; } = new List<Character>();
    public ICollection<Environment> Environments { get; } = new List<Environment>();
    public Character Player => Characters.First(c => c.IsPlayer);
    public bool IsOpenWorld { get; set; }
    public NarrativeGraph? NarrativeGraph { get; set; }
    public bool GameOver { get; set; } = false;

    /// <summary>
    /// Creates a deep copy of the campaign.
    /// </summary>
    /// <returns>The copy of the campaign.</returns>
    public Campaign DeepCopy()
    {
        var copy = new Campaign(
            User,
            Title,
            StartScenario ?? string.Empty,
            IsOpenWorld
        )
        {
            GameSummary = GameSummary,
            GameOver = GameOver,
        };

        copy.NarrativeGraph = NarrativeGraph?.DeepCopy(copy);

        foreach (var message in Messages)
        {
            copy.Messages.Add(message.DeepCopy(copy));
        }

        foreach (var environment in Environments)
        {
            copy.Environments.Add(new Environment(copy, environment.Name, environment.Description));
        }

        foreach (var character in Characters)
        {
            copy.Characters.Add(character.DeepCopy(copy,
                copy.Environments.First(e =>
                    e.Name == character.Environment.Name && e.Description == character.Environment.Description)));
        }

        return copy;
    }
}
