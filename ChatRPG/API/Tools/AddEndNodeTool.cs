using System.Text.Json;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;

namespace ChatRPG.API.Tools;

public class AddEndNodeTool(
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

        var addEndNodeInput =
            JsonSerializer.Deserialize<AddEndNodeInput>(ToolUtilities.RemoveMarkdown(input), JsonOptions) ??
            throw new JsonException("Failed to deserialize");

        if (!IsValidJson(addEndNodeInput, out var jsonValidationError))
        {
            return jsonValidationError ?? "Invalid JSON input.";
        }

        if (!TryAddEndNode(graph, addEndNodeInput, out var addEndNodeErrorMessage))
        {
            return addEndNodeErrorMessage ?? "Failed to add edge to end node.";
        }

        return $"The graph has been updated. From now on, use the updated graph:\n{graph.Serialize()}";
    }

    private static bool IsValidJson(AddEndNodeInput jsonNode, out string? errorMessage)
    {
        if (jsonNode.IsValid(out var errors))
        {
            errorMessage = null;
            return true;
        }

        errorMessage =
            $"Invalid input provided for the edge to the end node. Please correct the following errors:\n{string.Join("\n", errors)}";
        return false;
    }

    private static bool TryAddEndNode(NarrativeGraph graph, AddEndNodeInput newEndNode, out string? errorMessage)
    {
        var sourceNode = graph.Nodes.FirstOrDefault(n => n.Name == newEndNode.SourceNodeName);

        if (sourceNode is null)
        {
            errorMessage = $"Source node with name {newEndNode.SourceNodeName} not found.";
            return false;
        }

        var endNode = graph.Nodes.FirstOrDefault(n => n.Name == "End");

        if (endNode is null)
        {
            endNode = new NarrativeNode("End", "", graph);
            graph.AddNode(endNode);
        }

        sourceNode.Edges.Add(new NarrativeEdge(newEndNode.Conditions!, sourceNode, endNode));
        errorMessage = null;
        return true;
    }
}
