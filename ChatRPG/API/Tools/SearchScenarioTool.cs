using System.Text;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Databases.Postgres;
using LangChain.Extensions;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.API.Tools;

public class SearchScenarioTool(
    IConfiguration configuration,
    Campaign campaign,
    string name,
    string? description = null) : AgentTool(name, description)
{
    private readonly bool _shouldIncludePreviousMessages = configuration.GetValue<bool>("ShouldSummarize");

    public override async Task<string> ToolTask(string input, CancellationToken token = new CancellationToken())
    {
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        var embeddingModel = new TextEmbeddingV3SmallModel(provider);
        var llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.1 }
        };

        var vectorDatabase =
            new PostgresVectorDatabase(configuration.GetSection("ConnectionStrings")
                .GetValue<string>("DefaultConnection")!);
        var vectorCollection = await vectorDatabase.GetCollectionAsync("~collection-" + campaign.Id, token);

        var prompt = new StringBuilder();
        var summary = ToolUtilities.ConstructSummary(campaign, _shouldIncludePreviousMessages);
        prompt.Append(configuration.GetSection("SystemPrompts").GetValue<string>("SearchScenario")).Replace(
            "{summary}", summary);

        var inputSimilarDocuments =
            await vectorCollection.GetSimilarDocuments(embeddingModel, input, amount: 10, cancellationToken: token);

        var embeddings = new StringBuilder();
        embeddings.Append($"Context for input: {inputSimilarDocuments.AsString()}\n");

        var ongoingNodes = campaign.NarrativeGraph!.GetNodesWithStatus(NarrativeNode.Status.Ongoing);

        foreach (var node in ongoingNodes)
        {
            // Construct a query for the node based on the node's name, story content, and unvisited edges' conditions.
            // Note: Avoid using special formatting characters in the query because they may interfere
            // with the model's ability to extract scenario embeddings.
            var query =
                node.Name + " " +
                node.StoryContent + " " +
                string.Join(" ", node.Edges.Where(e => e.EdgeStatus == NarrativeEdge.Status.Unvisited)
                    .Select(e => e.Conditions));

            var nodeSimilarDocuments =
                await vectorCollection.GetSimilarDocuments(embeddingModel, query, amount: 10, cancellationToken: token);

            embeddings.Append($"Context for node {node.Name}: {nodeSimilarDocuments.AsString()}\n");
        }

        var chain = Set(input, "input")
                    | Set(embeddings.ToString(), "context")
                    | Set($"Scenario Graph:\n{campaign.NarrativeGraph!.Serialize()}\n", "graph")
                    | Template(prompt.ToString())
                    | LLM(llm);

        var response = await chain.RunAsync("text", cancellationToken: token);

        return response ?? "System: I'm sorry, I couldn't find any relevant scenarios.";
    }
}
