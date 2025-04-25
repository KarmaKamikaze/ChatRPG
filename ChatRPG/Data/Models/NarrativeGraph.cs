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

    public NarrativeNode? GetEndNode()
    {
        return Nodes.FirstOrDefault(node => node.Name == "End");
    }

    public NarrativeNode? GetStartNode()
    {
        return Nodes.FirstOrDefault(node => GetIncomingNodes(node).Count == 0);
    }

    public List<NarrativeNode> GetNodesWithStatus(NarrativeNode.Status status)
    {
        return Nodes.Where(n => n.NodeStatus == status).ToList();
    }

    public void InitializeStartNode()
    {
        var startNode = new NarrativeNode("Start", "", this);
        startNode.NodeStatus = NarrativeNode.Status.Ongoing;
        Nodes.Add(startNode);
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

    /// <summary>
    /// Creates a deep copy of the narrative graph.
    /// </summary>
    /// <param name="campaign">The new campaign snapshot that the graph is associated with.</param>
    /// <returns>The copy of the narrative graph.</returns>
    public NarrativeGraph DeepCopy(Campaign campaign)
    {
        var copy = new NarrativeGraph
        {
            Campaigns = new List<Campaign>() { campaign }
        };

        var nodeMap = new Dictionary<NarrativeNode, NarrativeNode>();

        foreach (var node in Nodes)
        {
            var newNode = new NarrativeNode(node.Name, node.StoryContent, copy)
            {
                NodeStatus = node.NodeStatus
            };
            nodeMap[node] = newNode;
            copy.Nodes.Add(newNode);
        }

        foreach (var oldNode in Nodes)
        {
            var newSourceNode = nodeMap[oldNode];
            foreach (var edge in oldNode.Edges)
            {
                var newEdge = new NarrativeEdge([.. edge.Conditions], newSourceNode, nodeMap[edge.TargetNode])
                {
                    EdgeStatus = edge.EdgeStatus
                };
                newSourceNode.Edges.Add(newEdge);
            }
        }

        return copy;
    }
}
