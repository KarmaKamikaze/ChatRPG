namespace ChatRPG.Data.Models;

public class Event
{
    public int Id { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public Character? Character { get; set; }
    public Environment Environment { get; set; } = null!;
    public EventType Type { get; set; } = EventType.Other;
    public string Description { get; set; } = null!;
    public int Ordering { get; set; } = 1;
}
