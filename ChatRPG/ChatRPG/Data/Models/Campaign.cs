namespace ChatRPG.Data.Models;

public class Campaign
{
    public int Id { get; set; }
    public StartScenario? StartScenario { get; set; }
    public User User { get; set; } = null!;
    public string? CustomStartScenario { get; set; }
    public string Title { get; set; } = null!;
    public DateTime StartedOn { get; set; }
}
