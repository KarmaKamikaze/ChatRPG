namespace ChatRPG.Data.Models;

public class Verdict
{
    private Verdict()
    {
    }

    public Verdict(Campaign campaign, string content)
    {
        Campaign = campaign;
        Content = content;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public string Content { get; private set; } = null!;
}
