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
    private const int BatchSize = 5;

    public ReActScribeAgent(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("ScribeReActPrompt"));
        _reActPrompt = configuration.GetSection("SystemPrompts").GetValue<string>("ScribeReActPrompt")!;
        _provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _scribeDebugMode = configuration.GetValue<bool>("ScribeChainDebug");
    }

    public async Task<NarrativeGraph> ScribeNarrativeGraph(byte[] uploadedFile, IProgress<int>? progress = null)
    {
        var graph = new NarrativeGraph();
        graph.InitializeStartNode();

        var pdfPig = new PdfPigPdfLoader();
        var documents = await pdfPig.LoadAsync(DataSource.FromBytes(uploadedFile));

        var llm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.4 }
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

            // Report progress
            var progressValue = (int)((i + BatchSize) / (double)documents.Count * 100);
            progress?.Report(Math.Min(progressValue, 100));
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
            "Do not use markdown! The tool requires valid JSON input structured as follows:\n " +
            "{ \"name\": \"a unique name of the node based on the location or plot point within the scenario document\", " +
            "\"storycontent\": \"the story content of the relevant details such as a description of the plot " +
            "point/location, key NPCs, obstacles, or possible discoveries, etc.\", \"edges\": [ { \"conditions\": " +
            "[ \"condition 1 for traversing the edge\", \"condition 2 for traversing the edge\" ], " +
            "\"sourcenodename\": \"the name of the source node that should be connected using this edge. " +
            "This node can already exist in the graph or it can be this node, if this node is the source\", " +
            "\"targetnodename\": \"the name of the target node that should be connected using this edge. " +
            "This node can already exist in the graph or it can be this node, if this node is the target \" } ] } " +
            "Each edge must include a list of conditions and " +
            "connect either from or to an existing node to maintain coherence in the narrative structure. " +
            "These conditions must be formulated as short easy-to-answer questions.\n\n " +
            "Example Usage:\n " +
            "Example 1: Gaining Information on The Abandoned Ruins\n " +
            "Scenario Context:\n " +
            "The player is currently at \"The Village of Eldermere\". A new story point is being introduced: " +
            "\"The Abandoned Ruins\", which contains an ancient shrine with hidden inscriptions. The player can " +
            "only proceed if they have spoken to the village elder and removed a large boulder in the way.\n " +
            "Tool Call Example:\n " +
            "{ \"name\": \"The Abandoned Ruins\", \"storycontent\": \"A crumbling stone structure overgrown " +
            "with vines, hiding an ancient shrine with faded inscriptions. The air is thick with mystery, and a " +
            "sense of forgotten history lingers. Possible discoveries include ancient artifacts and hidden passages.\", " +
            "\"edges\": [ { \"conditions\": [ \"Has the player spoken to the Village Elder?\", \"Has the player removed the large boulder?\" ], " +
            "\"sourcenodename\": \"The Village of Eldermere\", \"targetnodename\": \"The Abandoned Ruins\" } ] } " +
            "Expected Outcome:\n " +
            "The tool returns an updated string representation of the graph, now including \"The Abandoned Ruins\" " +
            "as a new node, connected to \"The Village of Eldermere\" via an edge with the conditions " +
            "\"Has the player spoken to the Village Elder?\" and \"Has the player removed the large boulder?\" You can now verify the structure and ensure " +
            "that traversal logic remains consistent with the scenario documents.\n " +
            "Example 2: Entering the Forbidden Archives (No Conditions Required)\n " +
            "A new node is added when the player discovers the Forbidden Archives, an ancient library containing lost knowledge. \n" +
            "{ \"name\": \"Forbidden Archives\", \"storycontent\": \"A vast underground library filled with " +
            "crumbling tomes, forbidden knowledge, and the echoes of long-forgotten scholars. " +
            "Strange symbols glow faintly on the walls, hinting at secrets waiting to be uncovered.\", " +
            "\"edges\": [ { \"conditions\": [], \"sourcenodename\": \"Grand Library\", \"targetnodename\": \"Forbidden Archives\" } ] } " +
            "Outcome:\n " +
            "- The Forbidden Archives is introduced as a new story node.\n " +
            "- The Grand Library is directly connected to it without conditions, meaning the player can freely enter the archives.\n " +
            "- The archives can now serve as a new exploration point with potential clues, puzzles, or hidden dangers.");
        tools.Add(addNodeTool);

        var addEdgeTool = new AddEdgeTool(graph, "addedgetool",
            "This tool must be used to add a new edge (connection) between two existing nodes in the " +
            "narrative graph. This tool should be used when you determine that a new pathway should be " +
            "established between two already-defined story points.\n\nEach edge represents a story-driven " +
            "connection between two nodes, allowing the player to progress based on specific conditions. " +
            "These conditions act as prerequisites that must be met before the player is allowed to traverse " +
            "the edge. An edge must include:\n " +
            "- A source node name, which is the starting point of the edge.\n " +
            "- A target node name, which is the destination of the edge.\n " +
            "- A list of conditions, which describe what the player must accomplish to traverse the edge.\n " +
            "Conditions should be framed as easy-to-answer questions, verifying if the player has completed " +
            "specific story requirements. These could be based on prior encounters, collected items, or " +
            "completed quests, such as:\n " +
            "- \"Has the player spoken to the village elder?\"\n " +
            "- \"Has the player recovered the stolen artifact from the crypt?\"\n " +
            "- \"Has the player defeated the guardian of the temple.\"\n " +
            "After calling this tool, you will receive an updated string representation of the graph, showing " +
            "the newly added edge and its connection between nodes. This allows you to verify relationships " +
            "and ensure logical story progression.\n " +
            "Usage Format:\n " +
            "Do not use markdown! The tool requires valid JSON input structured as follows:\n " +
            "{ \"conditions\": [ \"condition 1 for traversing the edge\", \"condition 2 for traversing the edge\" ], " +
            "\"sourcenodename\": \"the name of the source node which already exists in the graph\", " +
            "\"targetnodename\": \"the name of the target node which already exists in the graph\" }\n " +
            "Example 1: Unlocking the Crypt\n " +
            "In this scenario, the player must obtain the Rusted Key before they can enter the Ancient Crypt.\n " +
            "Input to AddEdgeTool:\n " +
            "{ \"sourcenodename\": \"Old Graveyard\", \"targetnodename\": \"Ancient Crypt\", \"conditions\": " +
            "[ \"Has the player obtained the Rusted Key?\" ] } Outcome:\n " +
            "- The Old Graveyard is now connected to the Ancient Crypt.\n " +
            "- The player cannot enter the crypt until they have obtained the Rusted Key.\n " +
            "Example 2: Gaining Access to the Royal Chamber\n " +
            "To enter the Royal Chamber, the player must have:\n " +
            "1. Met Sir Ivan, the Wizard, who provides the key to the chamber.\n " +
            "2. Defeated the Elite Guards stationed outside.\n " +
            "3. Dispelled the magical barrier on the Royal Chamber doors." +
            "Input to AddEdgeTool:\n " +
            "{ \"sourcenodename\": \"Castle Courtyard\", \"targetnodename\": \"Royal Chamber\", \"conditions\": " +
            "[ \"Has the player been granted the key by Sir Ivan, the Wizard?\", \"Has the player defeated the Elite Guards?\", \"Has the player dispelled the magical barrier?\" ] } " +
            "Outcome:\n " +
            "- The Castle Courtyard is now connected to the Royal Chamber.\n " +
            "- The player cannot enter until all conditions are fulfilled.");
        tools.Add(addEdgeTool);

        var addEndNodeTool = new AddEndNodeTool(graph, "addendnodetool",
            "This tool must be used to add a new end node to the narrative graph. An end node represents " +
            "a definitive conclusion to a story branch, meaning that once the player reaches this point, the " +
            "story will end. This tool must be used whenever a branch of the story does not loop back to another " +
            "plot point but instead results in a final outcome.\n " +
            "There can be multiple possible endings in an adventure scenario, so this tool must be invoked " +
            "whenever a narrative path leads to a conclusion instead of continuing forward. End nodes should be " +
            "used to signify significant story resolutions, such as:\n " +
            "- The player meeting their demise.\n " +
            "- The player achieving victory.\n " +
            "- The player failing or being trapped indefinitely.\n " +
            "- Any other scenario where the player's journey logically concludes.\n" +
            "Usage Format:\n " +
            "Do not use markdown! The tool requires valid JSON input structured as follows:\n " +
            "{ \"sourcenodename\": \"the name of the source node which already exists in the graph\", " +
            "\"conditions\": [ \"condition that define if the ending is reached based on the player’s choices\" ] }\n" +
            "Example Usage:\n " +
            "Example 1: A Hero’s Victory\n " +
            "If the player successfully defeats the Dark Lord and restores peace, the ending is triggered:\n " +
            "{ \"sourcenodename\": \"Victory Over the Dark Lord\", " +
            "\"conditions\": [ \"Has the player defeated the Dark Lord?\" ] } " +
            "Outcome:\n " +
            "- This ending is reached only if the player defeats the Dark Lord.\n " +
            "Example 2: The Player’s Demise\n " +
            "If the player fails to escape a collapsing dungeon:\n " +
            "{ \"sourcenodename\": \"Buried Beneath the Ruins\", " +
            "\"conditions\": [ \"Has the player failed to escape the ruins before time ran out?\" ] } " +
            "Outcome:\n " +
            "- The story ends when the player fails to escape the ruins.\n " +
            "Example 3: The Ascension of the New King\n " +
            "If the player successfully claims the throne by fulfilling multiple prerequisites:\n " +
            "{ \"sourcenodename\": \"Ascension to the Throne\", " +
            "\"conditions\": [ \"Has the player retrieved the Royal Crown?\", " +
            "\"Has the player gained the support of the High Council?\", " +
            "\"Has the player defeated the False Heir in battle?\" ] } " +
            "Outcome:\n " +
            "- This ending is only reached if the player has:\n " +
            "\t - Retrieved the Royal Crown, signifying their right to rule.\n " +
            "\t - Secured the High Council’s approval, ensuring political stability.\n " +
            "\t - Defeated the False Heir, eliminating rival claims to the throne.");
        tools.Add(addEndNodeTool);

        return tools;
    }
}
