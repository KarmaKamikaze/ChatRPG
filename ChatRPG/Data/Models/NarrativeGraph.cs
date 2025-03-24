using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatRPG.Data.Models;

public class NarrativeGraph
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

    [JsonIgnore]
    public int Id { get; private set; }

    [JsonIgnore]
    public ICollection<Campaign> Campaigns { get; private set; } = new List<Campaign>();

    public HashSet<NarrativeNode> Nodes { get; private set; } = [];

    public void AddNode(NarrativeNode node)
    {
        Nodes.Add(node);
    }

    public List<NarrativeNode> GetOutgoingNodes(NarrativeNode node)
    {
        return node.Edges.Select(edge => edge.TargetNode).ToList();
    }

    public List<NarrativeNode> GetIncomingNodes(NarrativeNode targetNode)
    {
        return Nodes.Where(n => n.Edges.Any(edge => edge.TargetNode == targetNode)).ToList();
    }

    public NarrativeNode? GetStartNode()
    {
        return Nodes.FirstOrDefault(node => GetIncomingNodes(node).Count == 0);
    }

    public void InitializeStartNode()
    {
        Nodes.Add(new NarrativeNode("Start", "", this));
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, _jsonSerializerOptions);
    }

    public void PrintGraph()
    {
        Console.WriteLine(this);
    }

    public override string ToString()
    {
        var startNode = GetStartNode();
        if (startNode == null)
        {
            return "Warning: No start node found. Create a node with no incoming edges.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Start Node: {startNode}\n");
        var visited = new HashSet<NarrativeNode>();

        Dfs(startNode);
        return sb.ToString();

        void Dfs(NarrativeNode node)
        {
            if (!visited.Add(node)) return; // Skip if already visited
            sb.AppendLine(node.ToString());
            foreach (var edge in node.Edges)
            {
                sb.AppendLine($"  {edge}");
                Dfs(edge.TargetNode); // Recursively visit adjacent nodes
            }
        }
    }
}
