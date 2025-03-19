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
    private readonly OpenAiProvider _provider;
    private readonly string _reActPrompt;
    private readonly bool _scribeDebugMode;
    private const int BatchSize = 10;

    public ReActScribeAgent(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("ScribeReActPrompt"));
        _reActPrompt = configuration.GetSection("SystemPrompts").GetValue<string>("ScribeReActPrompt")!;
        _provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _scribeDebugMode = configuration.GetValue<bool>("ScribeChainDebug");
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

        var previousGraphExtensionSummary =
            $"The starting node \"{graph.GetStartNode()!.Name}\" has been added as the beginning of the story.";

        for (var i = 0; i < documents.Count; i += BatchSize)
        {
            var agent = new ReActAgentChain(_scribeDebugMode ? llm.UseConsoleForDebug() : llm, graph,
                previousGraphExtensionSummary, reActPrompt: _reActPrompt, maxActions: 50);

            var tools = CreateTools(graph);
            foreach (var tool in tools)
            {
                agent.UseTool(tool);
            }

            var pages = string.Join("\n", documents.Skip(i).Take(BatchSize));

            var chain = Set(pages, "input") | agent;

            previousGraphExtensionSummary = await chain.RunAsync("text") ??
                                            $"No summary generated for the previous graph extension session. " +
                                            $"Here is the last summary available: {previousGraphExtensionSummary}";
        }

        return graph;
    }

    private static List<AgentTool> CreateTools(NarrativeGraph graph)
    {
        var tools = new List<AgentTool>();

        var addNodeTool = new AddNodeTool(graph, "addnodetool",
            "This tool must be used to add a new plot point (node) to the narrative graph while " +
            "structuring an adventure scenario. Each node represents a key location, event, or point of interest in " +
            "the story. The tool should be used whenever you determine that a new plot point needs to be " +
            "introduced in the graph based on the provided scenario document. Each node should contain:\n " +
            "- A unique name, which describes the location or plot point.\n " +
            "- A story content description, detailing the narrative aspects, such as key NPCs, obstacles, or " +
            "important discoveries.\n " +
            "- Edges, which represent the paths leading to or from other existing nodes.\n " +
            "\t- Each edge includes conditions that must be fulfilled before traversal is allowed.\n " +
            "\t- Edges connect an existing source node to the new target node or vice versa to ensure logical progression.\n " +
            "After calling this tool, you will receive an updated string representation of the narrative graph, " +
            "showing the newly added node and its connections, allowing you to verify relationships " +
            "between story points and ensure correct structuring.\n\n " +
            "Usage Format:\n " +
            "The tool requires valid JSON input structured as follows:\n " +
            "{ \"name\": \"a unique name of the node based on the location or plot point within the scenario document\", " +
            "\"storycontent\": \"the story content of the relevant details such as a description of the plot " +
            "point/location, key NPCs, obstacles, or possible discoveries, etc.\", \"edges\": [ { \"conditions\": " +
            "[ \"condition 1 for traversing the edge\", \"condition 2 for traversing the edge\" ], " +
            "\"sourcenodename\": \"the name of the source node that should be connected using this edge. " +
            "This node can already exist in the graph or it can be this node, if this node is the source\", " +
            "\"targetnodename\": \"the name of the target node that should be connected using this edge. " +
            "This node can already exist in the graph or it can be this node, if this node is the target \" } ] } " +
            "Each edge must include a list of conditions (which may be empty if no prerequisites exist) and " +
            "connect either from or to an existing node to maintain coherence in the narrative structure.\n\n " +
            "Example Usage:\n " +
            "Scenario Context:\n " +
            "The player is currently at \"The Village of Eldermere\". A new story point is being introduced: " +
            "\"The Abandoned Ruins\", which contains an ancient shrine with hidden inscriptions. The player can " +
            "only proceed if they have spoken to the village elder.\n " +
            "Tool Call Example:\n " +
            "{ \"name\": \"The Abandoned Ruins\", \"storycontent\": \"A crumbling stone structure overgrown " +
            "with vines, hiding an ancient shrine with faded inscriptions. The air is thick with mystery, and a " +
            "sense of forgotten history lingers. Possible discoveries include ancient artifacts and hidden passages.\", " +
            "\"edges\": [ { \"conditions\": [ \"Has the player spoken to the Village Elder?\" ], " +
            "\"sourcenodename\": \"The Village of Eldermere\", \"targetnodename\": \"The Abandoned Ruins\" } ] } " +
            "Expected Outcome:\n " +
            "The tool returns an updated string representation of the graph, now including \"The Abandoned Ruins\" " +
            "as a new node, connected to \"The Village of Eldermere\" via an edge with the condition " +
            "\"Has the player spoken to the Village Elder?\" You can now verify the structure and ensure " +
            "that traversal logic remains consistent with the scenario documents.");
        tools.Add(addNodeTool);

        var addEdgeTool = new AddEdgeTool(graph, "addedgetool", "Add an edge to the narrative graph");
        tools.Add(addEdgeTool);

        return tools;
    }
}
