using ChatRPG.API;
using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.Services;

public class ReActExaminerAgent
{
    private readonly IConfiguration _configuration;
    private readonly OpenAiProvider _provider;
    private readonly bool _examinerDebugMode;
    private readonly bool _shouldIncludePreviousMessages;
    private readonly string _reactPrompt;


    public ReActExaminerAgent(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("ExaminerReActPrompt"));
        _configuration = configuration;
        _provider = new OpenAiProvider(_configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _reactPrompt = _configuration.GetSection("SystemPrompts").GetValue<string>("ExaminerReActPrompt")!;
        _examinerDebugMode = _configuration.GetValue<bool>("ExaminerChainDebug");
        _shouldIncludePreviousMessages = _configuration.GetValue<bool>("ShouldSummarize");
    }


    public async Task<string> ExaminePlayerInput(Campaign campaign, string playerInput)
    {
        var llm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.4 }
        };

        var agent = new ReActAgentChain(_examinerDebugMode ? llm.UseConsoleForDebug() : llm, reactPrompt: _reactPrompt,
            gameSummary: ToolUtilities.ConstructSummary(campaign, _shouldIncludePreviousMessages),
            graph: campaign.NarrativeGraph);

        var tools = await CreateTools(campaign, playerInput);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var chain = Set(playerInput, "input") | agent;

        return (await chain.RunAsync("text"))!;
    }

    private async Task<List<AgentTool>> CreateTools(Campaign campaign, string playerInput)
    {
        var tools = new List<AgentTool>();

        var searchScenarioTool = await SearchScenarioTool.CreateAsync(
            _configuration,
            campaign,
            "searchscenariotool",
            """
            This tool must be used whenever you are unsure whether the player's proposed action is plausible, allowed, 
            or supported by the current scenario context—**before** any narrative is generated. It helps determine 
            whether a given interaction is reasonable based on the player's current position in the story, what has 
            been established so far, and what is possible within the structured world of the scenario document.

            The tool returns structured information about the adventure module to help guide your decision-making. 
            Use it to check what is available, what has been previously introduced, and whether the scenario supports 
            the player's intended action.

            When to use this tool:
            - **Uncertainty About Player Actions**: Use this tool when the player takes an action and you are unsure 
              if it’s possible or contextually supported by the scenario.
            - **Lack of Information**: If you do not know enough about the location, NPCs, quests, items, or 
              interactable objects in the current area, use this tool to retrieve relevant information before judging 
              whether the player's action is deemed feasible.
            - **Consistency With the Scenario Module**: If the scenario document might already contain important 
              details that could support or block the player’s intent, consult this tool before proceeding.
            - **Validation of Edge Traversal**: Use this tool to verify if a player’s action logically allows 
               progression to another node in the story graph—e.g., if edge conditions might be fulfilled, which should 
               be supported by the scenario document.
            - **Exploration & Interaction**: If the player takes an action related to an NPC, object, or location that has 
              not been described yet, use this tool to determine if their action is feasible.

            ### Input Format:
            Do not use markdown! 
            The tool requires a search query string, where you can inquire about the scenario module.
            Optionally, you may also provide a node name from the graph to help localize the search to a specific 
            story location. That could be the node where the player currently is if you want to learn
            more about the location, or the node where the player has been previously if you need important details 
            about a location that the player has already visited. Avoid leaking information about undiscovered nodes. 
            If you inquire about a future location, be very careful not to reveal plot points or NPCs the player has 
            not encountered yet.
            The input to this tool must be in the following RAW JSON format, where the "nodename" property is optional:
            {
                "query": "<The search query string>",
                "nodename": "<The name of the node in the graph>",
            }

            ### Example Uses:

            #### Scenario 1 – Castle Hall Inquiry
            **Player Input:** 'I want to talk to the ghost of the former king.'

            You are unsure if the scenario supports the existence of a ghost in this area. You call the tool with:  
            {
                "query": "Is there a ghost of the former king in the castle hall?",
                "nodename": "Castle Hall",
            }

            You retrieve details, learning that there is a suit of armor containing red eyes that greets the player as Ulemar, 
            the Knight of the King.

            #### Scenario 2 – Village House Exploration
            **Player Input:** 'I enter a random house in the village. What do I see?'

            You’re unsure if the houses are detailed in the scenario. You call the tool with:  
            {
                "query": "Tell me about the houses in the village.",
                "nodename": "Village Square",
            }

            If the adventure module contains details about the house, the tool retrieves them.  
            If the house is not mentioned, you may allow for minor furnishings, but do not invent major NPCs or plot points.

            #### Scenario 3 – Forest Departure
            **Player Input:** 'I leave the dungeon and wander into the forest.'

            Unsure what happens in the forest, you call:   
            {
                "query": "Tell me about the forest. Are there any objectives there? Does anything happen when the player leaves the dungeon?",
            }

            You retrieve information (if any) and make a decision about whether the player can go there yet or should 
            be nudged back to the dungeon.

            ---

            Use this tool as often as needed to maintain scenario consistency, validate potential actions, and confirm 
            whether the world logic supports the player's intent. Always prioritize established scenario content over 
            invention unless explicitly allowed.
            """);
        tools.Add(searchScenarioTool);

        var updateGraphTool = new UpdateGraphTool(
            _configuration,
            campaign,
            playerInput,
            "updategraphtool",
            """
            This tool must be used to evaluate whether the player can progress to a new story point by checking the 
            conditions of a potential transition between two plot nodes in the **narrative graph**.

            You should use this tool **whenever the player’s current input suggests a possible advancement** 
            in the story. If you deem the action is feasible and consistent with the plot and scenario, you 
            should query the graph to see if any edges leading to new nodes can be activated based on the current 
            state and fulfilled conditions.

            The tool checks if all required conditions are satisfied for the transition. If so, it updates the graph to 
            reflect the new story state, unlocking the next part of the adventure.

            > **Important:** Only use this tool after first determining that the player's input is reasonable and aligns 
            > with the scenario’s established logic, but before you give the final verdict in the final answer. 
            > When in doubt, it is often better to check than to miss a valid progression opportunity.

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

            When the tool fails to update the graph, use the information to guide the player based on the failed 
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
