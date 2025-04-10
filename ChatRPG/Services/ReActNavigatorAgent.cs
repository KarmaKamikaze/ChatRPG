using ChatRPG.API;
using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.Services;

public class ReActNavigatorAgent
{
    private readonly IConfiguration _configuration;
    private readonly Gpt4OmniModel _llm;
    private readonly bool _navigatorDebugMode;
    private readonly bool _shouldIncludePreviousMessages;
    private readonly string _reactPrompt;

    public ReActNavigatorAgent(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("NavigatorReActPrompt"));
        _configuration = configuration;
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.4 }
        };
        _reactPrompt = configuration.GetSection("SystemPrompts").GetValue<string>("NavigatorReActPrompt")!;
        _navigatorDebugMode = configuration.GetValue<bool>("NavigatorChainDebug");
        _shouldIncludePreviousMessages = configuration.GetValue<bool>("ShouldSummarize");
    }


    public async Task<string> ReviewGraph(Campaign campaign, string playerInput, string examinerVerdict)
    {
        var agent = new ReActAgentChain(_navigatorDebugMode ? _llm.UseConsoleForDebug() : _llm,
            reActPrompt: _reactPrompt,
            gameSummary: ToolUtilities.ConstructSummary(campaign, _shouldIncludePreviousMessages),
            graph: campaign.NarrativeGraph);

        var tools = CreateTools(campaign, playerInput, examinerVerdict);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var input = $"""
                     Player input: 
                     {playerInput}
                     Adherence verdict: 
                     {examinerVerdict}
                     """;
        var chain = Set(input, "input") | agent;

        return (await chain.RunAsync("text"))!;
    }

    private List<AgentTool> CreateTools(Campaign campaign, string playerInput, string examinerVerdict)
    {
        var tools = new List<AgentTool>();

        var updateGraphTool = new UpdateGraphTool(
            _configuration,
            campaign,
            playerInput,
            examinerVerdict,
            "updategraphtool",
            """
            This tool must be used to evaluate whether the player can progress to a new story point by checking the 
            conditions of a potential transition between two plot nodes in the **narrative graph**.

            You should use this tool **whenever the player’s current input suggests a possible advancement** 
            in the story. If the action is feasible and consistent with the plot and scenario, you should query the 
            graph to see if any edges leading to new nodes can be activated based on the current state and fulfilled 
            conditions.

            The tool checks if all required conditions are satisfied for the transition. If so, it updates the graph to 
            reflect the new story state, unlocking the next part of the adventure.

            > **Important:** Only use this tool after first determining that the player's input is reasonable and aligns 
            > with the scenario’s established logic. When in doubt, it is often better to check than to miss a 
            > valid progression opportunity.

            ### When to Use:
            - The player's action appears to fulfill narrative conditions that may open a new path in the story.
            - You are **unsure** if the input enables progression and needs to verify the edge conditions.
            - A decision must be made about whether to **update the graph** before passing control to the narrative-generating agent.

            ### Requirements:
            - There must be an **edge** between the two nodes.
            - The **source node** must be marked as **ongoing** or **completed**.
            - The **target node** must be **undiscovered**.

            If traversal is allowed, the tool:
            - Marks the **source node** as **completed**
            - Marks the **target node** as **ongoing**
            - Marks the **edge** as **visited**

            ---

            ### **Expected Input Format:**
            Use **RAW JSON format** (do not use markdown):
            {
                "sourcenodename": "<name of the source node>",
                "targetnodename": "<name of the target node>"
            }

            - **sourcenodename**: The current location or plot point the player is at.
            - **targetnodename**: The potential next location or plot point the player might reach.

            ---

            ### **Tool Output:**

            #### **If traversal is NOT allowed:**
            Returns a string showing each edge condition and whether it was met.
            
              **Example:**
              `"condition_1: true condition_2: false"`

            If any condition is false, the graph remains unchanged and cannot be updated for this node pair yet.

            #### **If traversal is ALLOWED:**
            The graph updates automatically:
            - Source node becomes completed
            - Target node becomes ongoing
            - The updated graph is returned for reference

            ---

            ### **Example 1: Advancement Allowed**
            **Scenario:**
            The player inserts a special key into a locked door within the **Ancient Crypt**.

            **Player Input:**  
            _"I insert the Ornate Crypt Key into the lock and push the door open."_

            The agent deems this input valid and feasible for story progression, so it calls the tool:
            {
                "sourcenodename": "Ancient Crypt Entrance",
                "targetnodename": "Hidden Chamber"
            }

            #### **Tool Output:**
            The graph updates successfully:
            A fully updated graph is returned because:
            `"Has the player used the Ornate Crypt Key?: true"`

            ---

            ### **Example 2: Advancement Blocked**
            **Scenario:**
            The player tries to cross a **Ruined Bridge** that must be reinforced first.

            **Player Input:**  
            _"I walk across the bridge slowly, testing each step."_

            The agent doubts whether the bridge is ready. It calls the tool to check:
            {
                "sourcenodename": "Ruined Bridge",
                "targetnodename": "Other Side of the Chasm"
            }

            **Tool Output:**  
            `"Has the player reinforced the bridge with sturdy materials?: false"`

            Since not all conditions are met, the graph is not updated.
            The following conditions have been checked and the results are returned:
            `"Has the player defeated the guardian of the bridge?: true Has the player reinforced the bridge with sturdy materials?: false"`

            When the tool fails to update the graph, use this information to guide the player based on the failed 
            conditions and requirements in a subtle way to avoid breaking immersion. Explain to the agent who generates 
            the narrative how this may be achieved.

            ---

            Use this tool to keep the story logic consistent, support dynamic progression, and ensure players only 
            unlock new plot points through meaningful, valid actions.
            """);
        tools.Add(updateGraphTool);

        return tools;
    }
}
