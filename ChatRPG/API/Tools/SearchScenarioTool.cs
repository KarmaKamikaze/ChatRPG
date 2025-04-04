using System.Text;
using System.Text.Json;
using ChatRPG.API.Tools.InputModels;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Databases;
using LangChain.Databases.Postgres;
using LangChain.Extensions;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.API.Tools;

public class SearchScenarioTool : AgentTool
{
    private readonly IConfiguration _configuration;
    private readonly Campaign _campaign;
    private readonly bool _shouldIncludePreviousMessages;
    private readonly TextEmbeddingV3SmallModel _embeddingModel;
    private readonly Gpt4OmniModel _llm;
    private IVectorCollection? _vectorCollection;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<SearchScenarioTool> CreateAsync(
        IConfiguration configuration,
        Campaign campaign,
        string name,
        string? description = null)
    {
        var searchScenarioTool = new SearchScenarioTool(configuration, campaign, name, description);

        var vectorDatabase =
            new PostgresVectorDatabase(configuration.GetSection("ConnectionStrings")
                .GetValue<string>("DefaultConnection")!);
        searchScenarioTool._vectorCollection = await vectorDatabase.GetCollectionAsync("~collection-" + campaign.Id);

        return searchScenarioTool;
    }

    private SearchScenarioTool(
        IConfiguration configuration,
        Campaign campaign,
        string name,
        string? description = null) : base(name, description)
    {
        _configuration = configuration;
        _campaign = campaign;
        _shouldIncludePreviousMessages = configuration.GetValue<bool>("ShouldSummarize");
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _embeddingModel = new TextEmbeddingV3SmallModel(provider);
        _llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.1 }
        };
    }

    public override async Task<string> ToolTask(string input, CancellationToken token = new CancellationToken())
    {
        try
        {
            var searchScenarioInput =
                JsonSerializer.Deserialize<SearchScenarioInput>(ToolUtilities.RemoveMarkdown(input), JsonOptions) ??
                throw new JsonException("Failed to deserialize");

            if (!IsValidJson(searchScenarioInput, out var jsonValidationError))
            {
                return jsonValidationError ?? "Invalid JSON input.";
            }

            string embeddingContext;

            if (searchScenarioInput.NodeName is null)
            {
                embeddingContext = await CreateEmbeddingContext(searchScenarioInput.Query!);
            }
            else
            {
                var queryNode = ValidateNode(searchScenarioInput.NodeName, out var nodeErrorMessage);
                if (queryNode is null)
                {
                    return nodeErrorMessage!;
                }

                embeddingContext = await CreateEmbeddingContext(searchScenarioInput.Query!, queryNode);
            }

            var prompt = new StringBuilder();
            var summary = ToolUtilities.ConstructSummary(_campaign, _shouldIncludePreviousMessages);
            prompt.Append(_configuration.GetSection("SystemPrompts").GetValue<string>("SearchScenario")).Replace(
                "{summary}", summary);

            var chain = Set(input, "input")
                        | Set(embeddingContext, "context")
                        | Set($"Scenario Graph:\n{_campaign.NarrativeGraph!.Serialize()}\n", "graph")
                        | Template(prompt.ToString())
                        | LLM(_llm);

            var response = await chain.RunAsync("text", cancellationToken: token);

            return response ?? "System: I'm sorry, I couldn't find any relevant scenarios.";
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

    private static bool IsValidJson(SearchScenarioInput jsonInput, out string? errorMessage)
    {
        if (jsonInput.IsValid(out var errors))
        {
            errorMessage = null;
            return true;
        }

        errorMessage =
            $"Invalid input provided for performing the search. Please correct the following errors:\n{string.Join("\n", errors)}";
        return false;
    }

    private NarrativeNode? ValidateNode(string nodeName, out string? errorMessage)
    {
        var node = _campaign.NarrativeGraph!.Nodes.FirstOrDefault(n => n.Name == nodeName);

        if (node is null)
        {
            errorMessage = $"Node with name {nodeName} not found.";
            return null;
        }

        errorMessage = null;
        return node;
    }

    private async Task<string> CreateEmbeddingContext(string input, NarrativeNode? queryNode = null)
    {
        var embeddings = new StringBuilder();

        var inputSimilarDocuments =
            await _vectorCollection!.GetSimilarDocuments(_embeddingModel, input, amount: 10);
        embeddings.Append($"Context for input: {inputSimilarDocuments.AsString()}\n");

        if (queryNode is not null)
        {
            var query =
                queryNode.Name + " " +
                queryNode.StoryContent + " " +
                string.Join(" ", queryNode.Edges.Where(e => e.EdgeStatus == NarrativeEdge.Status.Unvisited)
                    .Select(e => e.Conditions));

            var queryNodeSimilarDocuments =
                await _vectorCollection!.GetSimilarDocuments(_embeddingModel, query, amount: 10);
            embeddings.Append($"Context for node {queryNode.Name}: {queryNodeSimilarDocuments.AsString()}\n");

            var viableParents = _campaign.NarrativeGraph!.GetIncomingNodes(queryNode)
                .Where(n => n.NodeStatus != NarrativeNode.Status.Undiscovered).ToList();

            foreach (var node in viableParents)
            {
                // Construct a query for the node based on the node's name, story content, and the edge to the queryNode.
                // Note: Avoid using special formatting characters in the query because they may interfere
                // with the model's ability to extract scenario embeddings.
                var parentQuery =
                    node.Name + " " +
                    node.StoryContent + " " +
                    string.Join(" ", node.Edges.Where(e => e.TargetNode == queryNode)
                        .Select(e => e.Conditions));

                var nodeSimilarDocuments =
                    await _vectorCollection!.GetSimilarDocuments(_embeddingModel, parentQuery, amount: 10);

                embeddings.Append($"Context for node {node.Name}: {nodeSimilarDocuments.AsString()}\n");
            }
        }

        return embeddings.ToString();
    }
}
