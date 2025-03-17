using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatRPG.Data.Models;

public class NarrativeNode
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

    private NarrativeNode()
    {
    }

    public NarrativeNode(string name, string content, NarrativeGraph graph)
    {
        Name = name;
        StoryContent = content;
        Graph = graph;
    }

    [JsonIgnore]
    public int Id { get; private set; }
    [JsonIgnore]
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

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, _jsonSerializerOptions);
    }

    public override string ToString() => $"{NodeStatus} Node({Id}) named [{Name}]: {StoryContent}";
}
