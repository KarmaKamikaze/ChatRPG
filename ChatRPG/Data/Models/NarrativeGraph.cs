namespace ChatRPG.Data.Models;

public class NarrativeGraph
{
    public int Id { get; private set; }
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

    public void PrintGraph()
    {
        var startNode = GetStartNode();
        Console.WriteLine(startNode != null ? $"Start Node: {startNode}" : "Warning: No start node found.");

        foreach (var node in Nodes)
        {
            Console.WriteLine(node);
            foreach (var edge in node.Edges)
            {
                Console.WriteLine($"  {edge}");
            }
        }
    }
}
