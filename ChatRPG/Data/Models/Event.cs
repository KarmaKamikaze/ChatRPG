namespace ChatRPG.Data.Models;

public class Event
{
    private Event()
    {
    }

    public Event(Campaign campaign, Environment environment, EventType type, string name, string description)
    {
        Name = name;
        Campaign = campaign;
        Environment = environment;
        Type = type;
        Description = description;
    }

    public Event(Campaign campaign, Environment environment, EventType type, string name, string description, Character character)
        : this(campaign, environment, type, name, description)
    {
        Character = character;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public Character? Character { get; private set; }
    public Environment Environment { get; private set; } = null!;
    public EventType Type { get; private set; } = EventType.Other;
    public string Description { get; private set; } = null!;
    public int Ordering { get; private set; }
    public string Name { get; private set; } = null!;
}
