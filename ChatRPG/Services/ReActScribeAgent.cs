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
    private readonly Gpt4OmniModel _llm;
    private readonly string _reActPrompt;
    private readonly bool _scribeDebugMode;
    private const int BatchSize = 5;

    public ReActScribeAgent(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("ScribeReActPrompt"));
        _reActPrompt = configuration.GetSection("SystemPrompts").GetValue<string>("ScribeReActPrompt")!;
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.4 }
        };
        _scribeDebugMode = configuration.GetValue<bool>("ScribeChainDebug");
    }

    public async Task<NarrativeGraph> ScribeNarrativeGraph(byte[] uploadedFile, IProgress<int>? progress = null)
    {
        var graph = new NarrativeGraph();
        graph.InitializeStartNode();

        var pdfPig = new PdfPigPdfLoader();
        var documents = await pdfPig.LoadAsync(DataSource.FromBytes(uploadedFile));


        var previousGraphExtensionSummary =
            $"The starting node \"{graph.GetStartNode()!.Name}\" has been added as the beginning of the story.";

        for (var i = 0; i < documents.Count; i += BatchSize)
        {
            var agent = new ReActAgentChain(model: _scribeDebugMode ? _llm.UseConsoleForDebug() : _llm,
                reActPrompt: _reActPrompt, graph: graph, graphExtensionSummary: previousGraphExtensionSummary,
                maxActions: 50);

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

        var addNodeTool = new AddNodeTool(
            graph,
            "addnodetool",
            """
            This tool must be used to **add a new plot point** (node) to the narrative graph while 
            structuring an adventure scenario.  
            Each node represents a **key location**, **event**, or **point of interest** in the story. 
            The tool should be used whenever you determine that a new plot point needs to be introduced 
            in the graph based on the provided scenario document.

            ### **Node Contents**
            Each node should contain:
            - A **unique name**, which describes the location or plot point.
            - A **story content description**, detailing the narrative aspects, such as key NPCs, obstacles, or important discoveries.
            - **Edges**, which represent the paths leading to or from other existing nodes.
                - Each edge includes **conditions** that must be fulfilled before traversal is allowed.
                - Edges connect an existing source node to the new target node or vice versa to ensure logical progression.

            ---

            ### **After Using the Tool**
            After calling this tool, you will receive an updated string representation of the narrative graph, 
            showing the newly added node and its connections.  
            This allows you to **verify relationships** between story points and ensure **correct structuring**.

            ---

            ### **Usage Format**
            - **Do not use markdown!**  
            - The tool requires **valid JSON input**, structured as follows:
            {
                "name": "a unique name of the node based on the location or plot point within the scenario document",
                "storycontent": "the story content of the relevant details such as a description of the plot point/location, key NPCs, obstacles, or possible discoveries, etc.",
                "edges": [
                    {
                        "conditions": [
                            "condition 1 for traversing the edge",
                            "condition 2 for traversing the edge"
                        ],
                        "sourcenodename": "the name of the source node that should be connected using this edge. This node can already exist in the graph or it can be this node, if this node is the source",
                        "targetnodename": "the name of the target node that should be connected using this edge. This node can already exist in the graph or it can be this node, if this node is the target"
                    }
                ]
            }

            Each edge must include a **list of conditions** and connect either from or to an existing node to maintain coherence in the narrative structure.  
            These conditions must be formulated as **short easy-to-answer questions**.

            ---

            ### **Example Usage**
            #### **Example 1: Gaining Information on The Abandoned Ruins**
            **Scenario Context:**  
            The player is currently at "**The Village of Eldermere**". A new story point is being introduced: "**The Abandoned Ruins**", which contains an ancient shrine with hidden inscriptions. The player can only proceed if they have:
            - Spoken to the village elder.
            - Removed a large boulder blocking the path.

            #### **Tool Call Example:**
            {
                "name": "The Abandoned Ruins",
                "storycontent": "A crumbling stone structure overgrown with vines, hiding an ancient shrine with faded inscriptions. The air is thick with mystery, and a sense of forgotten history lingers. Possible discoveries include ancient artifacts and hidden passages.",
                "edges": [
                    {
                        "conditions": [
                            "Has the player spoken to the Village Elder?",
                            "Has the player removed the large boulder?"
                        ],
                        "sourcenodename": "The Village of Eldermere",
                        "targetnodename": "The Abandoned Ruins"
                    }
                ]
            }

            #### **Expected Outcome:**
            - The tool returns an updated string representation of the graph, now including "**The Abandoned Ruins**" as a new node, connected to "**The Village of Eldermere**" via an edge with the conditions:
              - "Has the player spoken to the Village Elder?"
              - "Has the player removed the large boulder?"
            - You can now verify the structure and ensure that traversal logic remains consistent with the scenario documents.

            ---

            #### **Example 2: Entering the Forbidden Archives (No Conditions Required)**
            **Scenario Context:**  
            A new node is added when the player discovers the **Forbidden Archives**, an ancient library containing lost knowledge.

            #### **Tool Call Example:**
            {
                "name": "Forbidden Archives",
                "storycontent": "A vast underground library filled with crumbling tomes, forbidden knowledge, and the echoes of long-forgotten scholars. Strange symbols glow faintly on the walls, hinting at secrets waiting to be uncovered.",
                "edges": [
                    {
                        "conditions": [],
                        "sourcenodename": "Grand Library",
                        "targetnodename": "Forbidden Archives"
                    }
                ]
            }

            #### **Outcome:**
            - The **Forbidden Archives** is introduced as a new story node.
            - The **Grand Library** is directly connected to it without conditions, meaning the player can freely enter the archives.
            - The archives can now serve as a new exploration point with potential clues, puzzles, or hidden dangers.
            """);
        tools.Add(addNodeTool);

        var addEdgeTool = new AddEdgeTool(
            graph,
            "addedgetool",
            """
            This tool must be used to **add a new edge** (connection) between two existing nodes in the narrative graph.  
            This tool should be used when you determine that a new pathway should be established between two already-defined story points.

            ### **What is an Edge?**
            Each edge represents a **story-driven connection** between two nodes, allowing the player to progress based on specific conditions.  
            These conditions act as **prerequisites** that must be met before the player is allowed to traverse the edge.

            An edge must include:
            - A **source node name**, which is the starting point of the edge.
            - A **target node name**, which is the destination of the edge.
            - A **list of conditions**, which describe what the player must accomplish to traverse the edge.

            ### **Conditions**
            Conditions should be framed as **easy-to-answer questions**, verifying if the player has completed specific story requirements. These could be based on prior encounters, collected items, or completed quests, such as:
            - "Has the player spoken to the village elder?"
            - "Has the player recovered the stolen artifact from the crypt?"
            - "Has the player defeated the guardian of the temple?"

            ---

            ### **After Using the Tool**
            After calling this tool, you will receive an updated string representation of the graph, showing the newly added edge and its connection between nodes.  
            This allows you to **verify relationships** and ensure **logical story progression**.

            ---

            ### **Usage Format**
            - **Do not use markdown!**  
            - The tool requires **valid JSON input**, structured as follows:
            {
                "conditions": [ "condition 1 for traversing the edge", "condition 2 for traversing the edge" ], 
                "sourcenodename": "the name of the source node which already exists in the graph", 
                "targetnodename": "the name of the target node which already exists in the graph" 
            }

            ---

            ### **Example Usage**
            #### **Example 1: Unlocking the Crypt**
            **Scenario:**  
            In this scenario, the player must obtain the Rusted Key before they can enter the Ancient Crypt.

            #### **Tool Input:**
            {
                "sourcenodename": "Old Graveyard", 
                "targetnodename": "Ancient Crypt", 
                "conditions": [ "Has the player obtained the Rusted Key?" ] 
            }

            #### **Outcome:**
            - The **Old Graveyard** is now connected to the **Ancient Crypt**.
            - The player cannot enter the crypt until they have obtained the Rusted Key.

            ---

            #### **Example 2: Gaining Access to the Royal Chamber**
            **Scenario:**  
            To enter the Royal Chamber, the player must have:
            1. Met Sir Ivan, the Wizard, who provides the key to the chamber.
            2. Defeated the Elite Guards stationed outside.
            3. Dispelled the magical barrier on the Royal Chamber doors.

            #### **Tool Input:**
            {
                "sourcenodename": "Castle Courtyard", 
                "targetnodename": "Royal Chamber", 
                "conditions": [ 
                    "Has the player been granted the key by Sir Ivan, the Wizard?", 
                    "Has the player defeated the Elite Guards?", 
                    "Has the player dispelled the magical barrier?" 
                    ] 
            }

            #### **Outcome:**
            - The **Castle Courtyard** is now connected to the **Royal Chamber**.
            - The player cannot enter until all conditions are fulfilled.
            """);
        tools.Add(addEdgeTool);

        var addEndNodeTool = new AddEndNodeTool(
            graph,
            "addendnodetool",
            """
            This tool must be used to **add a new end node** to the narrative graph.  
            An **end node** represents a **definitive conclusion** to a story branch, meaning that once the player reaches this point, the story will end.

            ### **When to Use This Tool**
            Use this tool **whenever a branch of the story does not loop back** to another plot point but instead results in a **final outcome**.  
            There can be **multiple possible endings** in an adventure scenario, so this tool must be invoked whenever a narrative path **leads to a conclusion** instead of continuing forward.

            End nodes should signify **significant story resolutions**, such as:
            - **The player meeting their demise.**
            - **The player achieving victory.**
            - **The player failing or being trapped indefinitely.**
            - **Any other scenario where the player's journey logically concludes.**

            ---

            ### **Usage Format**
            - **Do not use markdown!**  
            - The tool requires **valid JSON input**, structured as follows:
            {
                "sourcenodename": "the name of the source node which already exists in the graph", 
                "conditions": [ "condition that defines if the ending is reached based on the player’s choices" ]
            }

            ---

            ### **Example Usage**
            #### **Example 1: A Hero’s Victory**
            **Scenario:**  
            If the player successfully defeats the Dark Lord and restores peace, the ending is triggered.

            #### **Tool Input:**
            {
                "sourcenodename": "Victory Over the Dark Lord", 
                "conditions": [ "Has the player defeated the Dark Lord?" ] 
            }

            #### **Outcome:**
            - This ending is reached **only if the player defeats the Dark Lord**.

            ---

            #### **Example 2: The Player’s Demise**
            **Scenario:**  
            If the player fails to escape a collapsing dungeon, the story ends.

            #### **Tool Input:**
            {
                "sourcenodename": "Buried Beneath the Ruins", 
                "conditions": [ "Has the player failed to escape the ruins before time ran out?" ] 
            }

            #### **Outcome:**
            - The story **ends when the player fails to escape** the ruins.

            ---

            #### **Example 3: The Ascension of the New King**
            **Scenario:**  
            If the player successfully claims the throne by fulfilling multiple prerequisites, the ending is triggered.

            #### **Tool Input:**
            {
                "sourcenodename": "Ascension to the Throne",
                "conditions": [
                    "Has the player retrieved the Royal Crown?",
                    "Has the player gained the support of the High Council?",
                    "Has the player defeated the False Heir in battle?"
                ]
            }

            #### **Outcome:**
            - This ending is only reached if the player has:
                - **Retrieved the Royal Crown**, signifying their right to rule.
                - **Secured the High Council’s approval**, ensuring political stability.
                - **Defeated the False Heir**, eliminating rival claims to the throne.
            """);
        tools.Add(addEndNodeTool);

        return tools;
    }
}
