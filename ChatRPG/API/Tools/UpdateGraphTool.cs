using System.Text;
using System.Text.Json;
using ChatRPG.API.Response;
using ChatRPG.API.Tools.InputModels;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;

namespace ChatRPG.API.Tools;

public class UpdateGraphTool(
    IConfiguration configuration,
    Campaign campaign,
    string input,
    string name,
    string? description = null) : AgentTool(name, description)
{
    private readonly bool _shouldIncludePreviousMessages = configuration.GetValue<bool>("ShouldSummarize");
    private const int MaxRetryAttempts = 3;


    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override async Task<string> ToolTask(string input, CancellationToken token = new CancellationToken())
    {
        try
        {
            var updateGraphInput =
                JsonSerializer.Deserialize<UpdateGraphInput>(ToolUtilities.RemoveMarkdown(input), JsonOptions) ??
                throw new JsonException("Failed to deserialize");

            if (!IsValidJson(updateGraphInput, out var jsonValidationError))
            {
                return jsonValidationError ?? "Invalid JSON input.";
            }

            var sourceNode = ValidateSourceNode(updateGraphInput.SourceNodeName!, updateGraphInput.TargetNodeName!,
                out var sourceErrorMessage);
            var targetNode = ValidateTargetNode(updateGraphInput.TargetNodeName!, out var targetErrorMessage);

            if (sourceNode is null || targetNode is null)
            {
                return sourceErrorMessage + targetErrorMessage;
            }

            var edge = ValidateEdge(sourceNode, targetNode, out var edgeErrorMessage);

            if (edge is null)
            {
                return edgeErrorMessage!;
            }

            var response = await CheckConditions(edge);

            // If all retry attempt are spent, response and edge conditions are null. Therefore, instruct Archivist to try again.
            if (response?.EdgeConditions is null)
            {
                return "Failed to check conditions for updating the graph. Please try again.";
            }

            // If the conditions are not met, return a message with the conditions and their evaluations.
            if (response.EdgeConditions.Any(conditionResult => !conditionResult.Value))
            {
                var result = new StringBuilder();
                result.Append($"Some conditions were not met for updating the graph, and the node {targetNode.Name} " +
                              $"should therefore not be set to ongoing. " +
                              $"Here is the complete list of conditions and their evaluations:\n");
                foreach (var conditionResult in response.EdgeConditions)
                {
                    result.Append($"{conditionResult.Key}: {conditionResult.Value}\n");
                }

                return result.ToString();
            }

            // If the conditions are met, update the graph.
            edge.EdgeStatus = NarrativeEdge.Status.Visited;
            sourceNode.NodeStatus = NarrativeNode.Status.Completed;
            targetNode.NodeStatus = targetNode.Edges.Count == 0
                ? NarrativeNode.Status.Completed
                : NarrativeNode.Status.Ongoing;

            return $"Graph updated successfully. The source node {sourceNode.Name} is completed, and the target node " +
                   $"{targetNode.Name} is {targetNode.NodeStatusCategory}. " +
                   $"Here is the updated graph: \n{campaign.NarrativeGraph!.Serialize()}";
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

    private static bool IsValidJson(UpdateGraphInput jsonInput, out string? errorMessage)
    {
        if (jsonInput.IsValid(out var errors))
        {
            errorMessage = null;
            return true;
        }

        errorMessage =
            $"Invalid input provided for updating the graph. Please correct the following errors:\n{string.Join("\n", errors)}";
        return false;
    }

    private NarrativeNode? ValidateSourceNode(string sourceNodeName, string targetNodeName, out string? errorMessage)
    {
        var sourceNode = campaign.NarrativeGraph!.Nodes.FirstOrDefault(n => n.Name == sourceNodeName);

        if (sourceNode is null)
        {
            errorMessage = $"Source node with name {sourceNodeName} not found.";
            return null;
        }

        if (sourceNode.NodeStatus == NarrativeNode.Status.Undiscovered)
        {
            errorMessage = $"Source node {sourceNodeName} is not discovered. " +
                           $"Therefore, {targetNodeName} cannot be marked as ongoing. " +
                           "Cannot update the graph.";
            return null;
        }

        errorMessage = null;
        return sourceNode;
    }

    private NarrativeNode? ValidateTargetNode(string targetNodeName, out string? errorMessage)
    {
        var targetNode =
            campaign.NarrativeGraph!.Nodes.FirstOrDefault(n => n.Name == targetNodeName);
        if (targetNode is null)
        {
            errorMessage = $"Target node with name {targetNodeName} not found.";
            return null;
        }

        if (targetNode.NodeStatus != NarrativeNode.Status.Undiscovered)
        {
            errorMessage = $"Target node {targetNodeName} is already " +
                           (targetNode.NodeStatus == NarrativeNode.Status.Completed ? "completed" : "ongoing") +
                           ". Cannot update the graph.";
            return null;
        }

        errorMessage = null;
        return targetNode;
    }


    private static NarrativeEdge? ValidateEdge(NarrativeNode sourceNode, NarrativeNode targetNode,
        out string? errorMessage)
    {
        var edge = sourceNode.Edges.FirstOrDefault(e => e.TargetNode == targetNode);
        if (edge is null)
        {
            errorMessage = $"Edge from {sourceNode.Name} to {targetNode.Name} not found.";
            return null;
        }

        if (edge.EdgeStatus == NarrativeEdge.Status.Visited)
        {
            errorMessage = $"The edge from {sourceNode.Name} to {targetNode.Name} is already visited. " +
                           $"Cannot update the graph.";
            return null;
        }

        errorMessage = null;
        return edge;
    }

    private async Task<LlmResponseEdgeConditions?> CheckConditions(NarrativeEdge edge)
    {
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        var llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.1 },
        };

        var previousAttemptHistory = string.Empty;

        for (var i = 0; i < MaxRetryAttempts; i++)
        {
            var query = new StringBuilder();
            query.Append(configuration.GetSection("SystemPrompts").GetValue<string>("CheckGraphUpdateConditions")!
                .Replace("{graph}", campaign.NarrativeGraph!.Serialize())
                .Replace("{summary}", ToolUtilities.ConstructSummary(campaign, _shouldIncludePreviousMessages)
                                      + $"\n {input}")
                .Replace("{edge}", edge.Serialize())
                .Replace("{history}", previousAttemptHistory));

            var responseJson = await llm.GenerateAsync(query.ToString());

            var response = JsonSerializer.Deserialize<LlmResponseEdgeConditions>(
                ToolUtilities.RemoveMarkdown(responseJson.ToString()),
                JsonOptions);

            if (response?.EdgeConditions is null)
            {
                previousAttemptHistory +=
                    $"Response: {responseJson}\n Failure reason: Failed to deserialize the JSON response.\n";
                continue;
            }

            if (response.EdgeConditions.Count != edge.Conditions.Count)
            {
                previousAttemptHistory +=
                    $"Response: {responseJson}\n Failure reason: The number of conditions in the response is not equal to the amount of the edge's conditions.\n";
                continue;
            }

            if (response.EdgeConditions.Any(conditionResult =>
                    edge.Conditions.All(condition => conditionResult.Key != condition)))
            {
                previousAttemptHistory +=
                    $"Response: {responseJson}\n Failure reason: The response contains a condition not present in the edge's list of conditions.\n";
                continue;
            }

            return response;
        }

        return null;
    }
}
