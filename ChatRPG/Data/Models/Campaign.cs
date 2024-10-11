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
    public string GameSummary { get; set; } = string.Empty;
    public ICollection<Character> Characters { get; } = new List<Character>();
    public ICollection<Environment> Environments { get; } = new List<Environment>();
    public Character Player => Characters.First(c => c.IsPlayer);

    public Character InsertOrUpdateCharacter(Character character)
    {
        Character? existingChar = Characters.FirstOrDefault(c => c.Name.ToLower().Equals(character.Name.ToLower()));
        if (existingChar != null)
        {
            existingChar.Description = character.Description;
            return existingChar;
        }
        Characters.Add(character);
        return character;
    }

    public Environment InsertOrUpdateEnvironment(Environment environment)
    {
        Environment? existing = Environments.FirstOrDefault(e => e.Name.ToLower().Equals(environment.Name.ToLower()));
        if (existing != null)
        {
            existing.Description = environment.Description;
            return existing;
        }
        Environments.Add(environment);
        return environment;
    }
}
