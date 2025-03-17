using ChatRPG.API;
using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.DocumentLoaders;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.Services;

public class ReActScribeAgent
{
    private readonly IConfiguration _configuration;
    private readonly OpenAiProvider _provider;
    private readonly string _reActPrompt;
    private readonly bool _scribeDebugMode;
    private const int BatchSize = 10;

    public ReActScribeAgent(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts").GetValue<string>("ScribeReActPrompt"));
        _configuration = configuration;
        _reActPrompt = _configuration.GetSection("SystemPrompts").GetValue<string>("ScribeReActPrompt")!;
        _provider = new OpenAiProvider(_configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _scribeDebugMode = _configuration.GetValue<bool>("ScribeChainDebug");
    }

    public async Task<NarrativeGraph> ScribeNarrativeGraph(byte[] uploadedFile)
    {
        var graph = new NarrativeGraph();
        graph.InitializeStartNode();

        var pdfPig = new PdfPigPdfLoader();
        var documents = await pdfPig.LoadAsync(DataSource.FromBytes(uploadedFile));

        var llm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.7 }
        };

        for (var i = 0; i < documents.Count; i+=BatchSize)
        {
            var agent = new ReActAgentChain(_scribeDebugMode ? llm.UseConsoleForDebug() : llm, graph, reActPrompt: _reActPrompt);

            var tools = CreateTools(graph);
            foreach (var tool in tools)
            {
                agent.UseTool(tool);
            }

            var pages = string.Join("\n", documents.Skip(i).Take(BatchSize));

            var chain = Set(pages, "input") | agent;

            await chain.RunAsync("text");
        }

        throw new NotImplementedException();
    }

    private static List<AgentTool> CreateTools(NarrativeGraph graph)
    {
        var tools = new List<AgentTool>();

        var addNodeTool = new AddNodeTool(graph, "addnodetool", "Add a node to the narrative graph");
        tools.Add(addNodeTool);


        return tools;
    }



}
