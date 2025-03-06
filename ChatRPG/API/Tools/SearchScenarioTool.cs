using System.Text;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Databases.Postgres;
using LangChain.Providers;
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
                .GetValue<string>("DefaultConnection")!, configuration.GetValue<string>("VectorDatabaseTable")!);
        var vectorCollection = await vectorDatabase.GetCollectionAsync("collection-" + campaign.Id, token);

        var prompt = new StringBuilder();
        var summary = ToolUtilities.ConstructSummary(campaign, _shouldIncludePreviousMessages);
        prompt.Append(configuration.GetSection("SystemPrompts").GetValue<string>("SearchScenario"))!.Replace(
            "{summary}", summary);

        var chain = Set(input, "input")
                    | RetrieveSimilarDocuments(vectorCollection, embeddingModel, inputKey: "input", amount: 20)
                    | CombineDocuments(outputKey: "context")
                    | Template(prompt.ToString())
                    | LLM(llm.UseConsoleForDebug()); // TODO: Remove debug mode

        var response = await chain.RunAsync("text", cancellationToken: token);

        return response ?? "System: I'm sorry, I couldn't find any relevant scenarios.";
    }
}
