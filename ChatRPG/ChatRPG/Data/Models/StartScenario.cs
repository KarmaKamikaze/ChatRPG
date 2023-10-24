namespace ChatRPG.Data.Models;

public class StartScenario
{
    private StartScenario() {}

    public StartScenario(string title, string body)
    {
        Title = title;
        Body = body;
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public ICollection<Campaign> Campaigns { get; } = new List<Campaign>();
}
