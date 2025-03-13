using System.Text;

namespace ChatRPG.Data.Models;

public class NarrativeEdge
{
    private NarrativeEdge()
    {
    }

    public NarrativeEdge(List<string> conditions, NarrativeNode sourceNode, NarrativeNode targetNode)
    {
        Conditions = conditions;
        SourceNode = sourceNode;
        TargetNode = targetNode;
    }

    public int Id { get; private set; }
    public ICollection<string> Conditions { get; private set; }
    public int SourceNodeId { get; private set; }
    public NarrativeNode SourceNode { get; private set; }
    public int TargetNodeId { get; private set; }
    public NarrativeNode TargetNode { get; private set; }
    public Status EdgeStatus { get; private set; } = Status.Unvisited;

    public enum Status
    {
        Unvisited,
        Visited
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"{EdgeStatus} Edge from {SourceNode.Id} to {TargetNode.Id} with conditions: ");
        sb.Append(string.Join(", ", Conditions));

        return sb.ToString();
    }
}
