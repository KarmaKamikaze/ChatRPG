using System.Text.Json;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;

namespace ChatRPG.API.Tools;

public class AddEdgeTool(
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

        var addEdgeInput =
            JsonSerializer.Deserialize<AddEdgeInput>(ToolUtilities.RemoveMarkdown(input), JsonOptions) ??
            throw new JsonException("Failed to deserialize");

        if (!IsValidJson(addEdgeInput, out var jsonValidationError))
        {
            return jsonValidationError ?? "Invalid JSON input.";
        }

        if (!TryAddEdge(graph, addEdgeInput, out var addEdgeErrorMessage))
        {
            return addEdgeErrorMessage ?? "Failed to add edge.";
        }

        return $"The graph has been updated. From now on, use the updated graph:\n{graph.Serialize()}";
    }

    private static bool IsValidJson(AddEdgeInput jsonEdge, out string? errorMessage)
    {
        if (jsonEdge.IsValid(out var errors))
        {
            errorMessage = null;
            return true;
        }

        errorMessage =
            $"Invalid input provided for the edge. Please correct the following errors:\n{string.Join("\n", errors)}";
        return false;
    }

    private static bool TryAddEdge(NarrativeGraph graph, AddEdgeInput newEdge, out string? errorMessage)
    {
        var existingNodes = graph.Nodes.ToDictionary(n => n.Name);

        existingNodes.TryGetValue(newEdge.SourceNodeName!, out var sourceNode);
        existingNodes.TryGetValue(newEdge.TargetNodeName!, out var targetNode);

        if (!ToolUtilities.NodesValidForNewEdge(sourceNode, targetNode, newEdge, out var errorMessages))
        {
            errorMessage =
                $"Invalid input provided for the edge. " +
                $"Please correct the following errors:\n{string.Join("\n", errorMessages)}";
            return false;
        }

        sourceNode!.Edges.Add(new NarrativeEdge(newEdge.Conditions!, sourceNode, targetNode!));
        errorMessage = null;
        return true;
    }
}
