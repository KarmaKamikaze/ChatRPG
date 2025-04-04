using System.Text.Json;
using ChatRPG.API.Tools.InputModels;
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

        try
        {
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

            return $"The graph has been updated. Examine the graph to determine if additional edges should be " +
                   $"added based on the newly added node. From now on, use the updated graph:\n{graph.Serialize()}";
        }
        catch (JsonException ex)
        {
            return $"Failed to deserialize the input. Please ensure the input is valid JSON. Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred: {ex.Message}";
        }
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

            if (ToolUtilities.NodesValidForNewEdge(sourceNode, targetNode, edge, out errorMessages))
            {
                edgesToAdd.Add(new NarrativeEdge(edge.Conditions!, sourceNode!, targetNode!));
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
