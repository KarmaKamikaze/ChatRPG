using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatRPG.Data.Models;

public class NarrativeEdge
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

    private NarrativeEdge()
    {
    }

    public NarrativeEdge(List<string> conditions, NarrativeNode sourceNode, NarrativeNode targetNode)
    {
        Conditions = conditions;
        SourceNode = sourceNode;
        TargetNode = targetNode;
    }

    [JsonIgnore]
    public int Id { get; private set; }

    public ICollection<string> Conditions { get; private set; }

    [JsonIgnore]
    public int SourceNodeId { get; private set; }

    public string SourceNodeName => SourceNode.Name;

    [JsonIgnore]
    public NarrativeNode SourceNode { get; private set; }

    [JsonIgnore]
    public int TargetNodeId { get; private set; }

    public string TargetNodeName => TargetNode.Name;

    [JsonIgnore]
    public NarrativeNode TargetNode { get; private set; }

    [JsonIgnore]
    public Status EdgeStatus { get; private set; } = Status.Unvisited;

    public string EdgeStatusCategory => EdgeStatus.ToString();

    public enum Status
    {
        Unvisited,
        Visited
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, _jsonSerializerOptions);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"{EdgeStatus} Edge from {SourceNodeName} to {TargetNodeName} with conditions: ");
        sb.Append(string.Join(", ", Conditions));

        return sb.ToString();
    }
}
