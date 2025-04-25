using System.Text;
using ChatRPG.Data.Models;
using LangChain.Databases.Postgres;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using LangChain.DocumentLoaders;
using LangChain.Extensions;
using LangChain.Splitters.Text;
using Npgsql;
using static LangChain.Chains.Chain;

namespace ChatRPG.Services;

public class ScenarioDocumentService
{
    private readonly ILogger<ScenarioDocumentService> _logger;
    private readonly string _connectionString;
    private readonly string _openAiKey;
    private readonly string _startingScenarioPrompt;

    public ScenarioDocumentService(IConfiguration configuration, ILogger<ScenarioDocumentService> logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ConnectionStrings")
            .GetValue<string>("DefaultConnection"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("StartingScenario"));
        _connectionString = configuration.GetSection("ConnectionStrings")
            .GetValue<string>("DefaultConnection")!;
        _openAiKey = configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!;
        _startingScenarioPrompt = configuration.GetSection("SystemPrompts").GetValue<string>("StartingScenario")!;
        _logger = logger;
    }

    public async Task StoreScenarioEmbedding(int campaignId, byte[] scenarioDocument)
    {
        var provider = new OpenAiProvider(_openAiKey);
        var embeddingModel = new TextEmbeddingV3SmallModel(provider);

        var vectorDatabase = new PostgresVectorDatabase(_connectionString);

        _ = await vectorDatabase.AddDocumentsFromAsync<PdfPigPdfLoader>(
            embeddingModel,
            dimensions: 1536,
            dataSource: DataSource.FromBytes(scenarioDocument),
            collectionName: "~collection-" + campaignId,
            // Configure how to extract chunks from the bigger document.
            textSplitter: new RecursiveCharacterTextSplitter(
                chunkSize: 500, // To pick the chunk size, estimate how much information would be required to capture most passages you'd like to ask questions about.  Too many characters makes it difficult to capture semantic meaning, and too few characters means you are more likely to split up important points that are related. In general, 200-500 characters is good for stories without complex sequences of actions.
                chunkOverlap: 200)); // To pick the chunk overlap you need to estimate the size of the smallest piece of information. It may happen that one chunk ends with `Ron's hair` and the other one starts with `is red`.In this case, an embedding would miss important context, and not be generated properly. With overlap the end of the first chunk will appear in the beginning of the other, eliminating the problem.
    }

    public async Task<string> GenerateStartingScenario(Campaign campaign)
    {
        var provider = new OpenAiProvider(_openAiKey);
        var embeddingModel = new TextEmbeddingV3SmallModel(provider);
        var llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 1 }
        };

        var vectorDatabase =
            new PostgresVectorDatabase(_connectionString);
        var vectorCollection = await vectorDatabase.GetCollectionAsync("~collection-" + campaign.Id);

        var prompt = new StringBuilder();
        prompt.Append(_startingScenarioPrompt);

        var chain = Set(CreateRagQueryForStartingScenario(campaign), outputKey: "query")
                    | RetrieveSimilarDocuments(vectorCollection, embeddingModel, inputKey: "query", amount: 20)
                    | CombineDocuments(outputKey: "context")
                    | Template(prompt.ToString())
                    | LLM(llm);

        var response = await chain.RunAsync("text");

        return response ?? "System: I'm sorry, I couldn't find any relevant scenarios.";
    }

    public async Task CopyScenarioEmbeddingForSnapshot(int oldCampaignId, int newCampaignId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sourceTable = $"~collection-{oldCampaignId}";
        var targetTable = $"~collection-{newCampaignId}";

        // Check if the source table exists
        var existsCmd = new NpgsqlCommand(
            """
                        SELECT EXISTS (
                            SELECT FROM pg_tables 
                            WHERE schemaname = 'public' AND tablename = @table
                        );
            """, conn);
        existsCmd.Parameters.AddWithValue("table", sourceTable);

        var exists = await existsCmd.ExecuteScalarAsync();
        if (exists != null && !(bool)exists)
        {
            _logger.LogWarning("Source table '{SourceTable}' does not exist", sourceTable);
            return;
        }

        // Create the new table as a copy
        var copyCmd = new NpgsqlCommand(
            $"""
                         CREATE TABLE "{targetTable}" AS TABLE "{sourceTable}";
             """, conn);

        await copyCmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Created table '{TargetTable}' as a copy of '{SourceTable}'", targetTable, sourceTable);
    }

    private static string CreateRagQueryForStartingScenario(Campaign campaign)
    {
        var ragQuery = new StringBuilder();
        ragQuery.AppendLine("Adventure Introduction.");

        var startNode = campaign.NarrativeGraph!.GetStartNode();

        foreach (var edge in startNode!.Edges)
        {
            ragQuery.AppendLine(string.Join(", ", edge.Conditions));
            ragQuery.AppendLine(edge.TargetNodeName);
            ragQuery.AppendLine(edge.TargetNode.StoryContent);
        }

        return ragQuery.ToString();
    }
}
