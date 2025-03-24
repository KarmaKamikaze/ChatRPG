using ChatRPG.Data.Models;
using Rubjerg.Graphviz;

namespace ChatRPG.Visualization;

public static class GraphVisualization
{
    /// <summary>
    /// Saves the narrative graph to a DOT file.
    /// </summary>
    /// <param name="narrativeGraph">The narrative graph, containing nodes and edges representing a story.</param>
    public static void SaveToDotFile(NarrativeGraph narrativeGraph)
    {
        Directory.CreateDirectory("./Visualizations"); // Create the directory if it doesn't exist
        var graph = ConstructGraph(narrativeGraph);
        graph.ToDotFile($"./Visualizations/{graph.GetName()}.dot");
    }

    /// <summary>
    /// Saves the narrative graph to a SVG file.
    /// </summary>
    /// <param name="narrativeGraph">The narrative graph, containing nodes and edges representing a story.</param>
    public static void SaveToSvgFile(NarrativeGraph narrativeGraph)
    {
        Directory.CreateDirectory("./Visualizations"); // Create the directory if it doesn't exist
        var graph = ConstructGraph(narrativeGraph);
        graph.ComputeLayout(); // default is 'dot' layout
        graph.ToSvgFile($"./Visualizations/{graph.GetName()}.svg");
    }

    /// <summary>
    /// Saves the narrative graph to a PNG file.
    /// </summary>
    /// <param name="narrativeGraph">The narrative graph, containing nodes and edges representing a story.</param>
    public static void SaveToPngFile(NarrativeGraph narrativeGraph)
    {
        Directory.CreateDirectory("./Visualizations"); // Create the directory if it doesn't exist
        var graph = ConstructGraph(narrativeGraph);
        graph.ComputeLayout(); // default is 'dot' layout
        graph.ToPngFile($"./Visualizations/{graph.GetName()}.png");
    }

    /// <summary>
    /// Constructs a GraphViz graph from a narrative graph.
    /// </summary>
    /// <param name="graph">The narrative graph, containing nodes and edges representing a story.</param>
    /// <returns>The root node of a GraphViz graph, representing the narrative graph.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If either the NodeStatus or the EdgeStatus are out of range.</exception>
    private static RootGraph ConstructGraph(NarrativeGraph graph)
    {
        var root = RootGraph.CreateNew(GraphType.Directed, $"Graph ID {graph.Id}");
        Node.IntroduceAttribute(root, "shape", "circle");
        Edge.IntroduceAttribute(root, "label", "");

        foreach (var node in graph.Nodes)
        {
            var newNode = root.GetOrAddNode(node.Id.ToString());

            // If the node is the start node, make it a point,
            // otherwise set the label to the story content
            if (node.Id == graph.GetStartNode()?.Id)
            {
                newNode.SetAttribute("shape", "point");
            }
            else
            {
                newNode.SafeSetAttribute("label", node.Name, "");

                switch (node.NodeStatus)
                {
                    case NarrativeNode.Status.Undiscovered:
                        newNode.SetAttribute("color", "red");
                        break;
                    case NarrativeNode.Status.Ongoing:
                        newNode.SetAttribute("color", "yellow");
                        break;
                    case NarrativeNode.Status.Completed:
                        newNode.SetAttribute("color", "green");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        foreach (var node in graph.Nodes)
        {
            foreach (var edge in node.Edges)
            {
                var source = root.GetNode(edge.SourceNodeId.ToString());
                var target = root.GetNode(edge.TargetNodeId.ToString());

                var newEdge = root.GetOrAddEdge(source, target, edge.Id.ToString());
                newEdge.SafeSetAttribute("label", string.Join(", ", edge.Conditions), "");

                switch (edge.EdgeStatus)
                {
                    case NarrativeEdge.Status.Unvisited:
                        newEdge.SetAttribute("color", "red");
                        break;
                    case NarrativeEdge.Status.Visited:
                        newEdge.SetAttribute("color", "green");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        return root;
    }
}
