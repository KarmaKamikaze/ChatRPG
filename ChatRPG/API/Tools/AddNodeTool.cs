using System.Text.Json;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;

namespace ChatRPG.API.Tools;

public class AddNodeTool(
    NarrativeGraph graph,
    string name,
    string? description = null) : AgentTool(name, description)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };


    public override async Task<string> ToolTask(string input, CancellationToken token = new CancellationToken())
    {
        await Task.Yield();

        var addNodeInput =
            JsonSerializer.Deserialize<AddNodeInput>(ToolUtilities.RemoveMarkdown(input), JsonOptions) ??
            throw new JsonException("Failed to deserialize");

        if (!IsValidJson(addNodeInput, out var jsonValidationError))
        {
            return jsonValidationError ?? "Invalid JSON input.";
        }

        if (!TryAddNode(graph, addNodeInput, out var addNodeErrorMessage))
        {
            return addNodeErrorMessage ?? "Failed to add node.";
        }

        return $"The graph has been updated. From now on, use the updated graph:\n{graph.Serialize()}";
    }

    private static bool IsValidJson(AddNodeInput jsonNode, out string? errorMessage)
    {
        if (jsonNode.IsValid(out var errors))
        {
            errorMessage = null;
            return true;
        }

        errorMessage =
            $"Invalid input provided for the node. Please correct the following errors:\n{string.Join("\n", errors)}";
        return false;
    }

    private static bool TryAddNode(NarrativeGraph graph, AddNodeInput newNode, out string? errorMessage)
    {
        if (graph.Nodes.Any(n => n.Name == newNode.Name))
        {
            errorMessage = "Node with the same name already exists. Please provide a unique name.";
            return false;
        }

        var node = new NarrativeNode(newNode.Name!, newNode.StoryContent!, graph);
        var edgesToAdd = new List<NarrativeEdge>();
        var existingNodes = graph.Nodes.ToDictionary(n => n.Name);

        var errorMessages = new List<string>();

        foreach (var edge in newNode.Edges!)
        {
            existingNodes.TryGetValue(edge.SourceNodeName!, out var sourceNode);
            existingNodes.TryGetValue(edge.TargetNodeName!, out var targetNode);

            sourceNode ??= edge.SourceNodeName == newNode.Name ? node : null;
            targetNode ??= edge.TargetNodeName == newNode.Name ? node : null;

            if (targetNode is null)
            {
                errorMessages.Add($"Target node with name {edge.TargetNodeName} not found.");
            }

            if (sourceNode is null)
            {
                errorMessages.Add($"Source node with name {edge.SourceNodeName} not found.");
            }
            else if (sourceNode == targetNode)
            {
                errorMessages.Add($"Node {sourceNode.Name} cannot have an edge to itself.");
            }
            else if (targetNode is not null && sourceNode.Edges.Any(e => e.TargetNode == targetNode))
            {
                errorMessages.Add($"An edge already exists between {edge.SourceNodeName} and {edge.TargetNodeName}.");
            }
            else if (targetNode is not null)
            {
                edgesToAdd.Add(new NarrativeEdge(edge.Conditions!, sourceNode, targetNode));
            }
        }

        if (errorMessages.Count != 0)
        {
            errorMessage =
                $"Invalid input provided for the node. " +
                $"Please correct the following errors:\n{string.Join("\n", errorMessages)}";
            return false;
        }

        graph.AddNode(node);
        foreach (var edge in edgesToAdd)
        {
            edge.SourceNode.Edges.Add(edge);
        }

        errorMessage = null;
        return true;
    }
}
