using System.Text.Json;

namespace ChatRPG.Data.Models;

public class NarrativeNode
{
    private NarrativeNode()
    {
    }

    public NarrativeNode(string name, string content, NarrativeGraph graph)
    {
        Name = name;
        StoryContent = content;
        Graph = graph;
    }

    public int Id { get; private set; }
    public NarrativeGraph Graph { get; private set; } = null!;
    public string Name { get; private set; }
    public string StoryContent { get; set; }
    public ICollection<NarrativeEdge> Edges { get; private set; } = [];
    public Status NodeStatus { get; private set; } = Status.Undiscovered;

    public enum Status
    {
        Undiscovered,
        Ongoing,
        Completed
    }

    public void AddCondition(List<string> conditions, NarrativeNode targetNode)
    {
        Edges.Add(new NarrativeEdge(conditions, this, targetNode));
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public override string ToString() => $"{NodeStatus} Node({Id}): {StoryContent}";
}
