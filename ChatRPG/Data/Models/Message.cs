namespace ChatRPG.Data.Models;

public class Message
{
    private Message()
    {
    }

    public Message(Campaign campaign, MessageRole role, string content, Verdict? verdict = null)
    {
        Campaign = campaign;
        Role = role;
        Content = content;
        Timestamp = DateTime.UtcNow;
        Verdict = verdict;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public MessageRole Role { get; private set; } = MessageRole.User;
    public string Content { get; private set; } = null!;
    public DateTime Timestamp { get; private set; }
    public Verdict? Verdict { get; private set; }
}
