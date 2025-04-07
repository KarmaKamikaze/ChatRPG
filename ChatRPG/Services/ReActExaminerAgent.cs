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
            This tool must be used whenever you are unsure of what is available to the player in the 
            current location, uncertain about what should happen next, or need to reference existing details from 
            the adventure module to maintain consistency. The tool helps you retrieve structured information 
            about the game world, ensuring it adheres to the story's established details while still allowing for 
            player agency and exploration.

            When to use this tool:
            - **Unknown Details**: If you do not have enough information about a location, NPC, quest, faction, or 
              available actions, this tool must be used to find relevant context from the adventure module.
            - **Player Agency & Story Consistency**: You should follow the scenario structure but can adapt if minor 
              details are missing. However, if key details exist in the adventure module, they must be used to shape 
              the game world.
            - **Keeping the Player on Track**: If the player strays too far from the main story while exploring an area, 
              the tool can be used to find details that naturally guide them back into the intended narrative without 
              restricting their choices.
            - **Exploration & Interaction**: If the player takes an action related to an NPC, object, or location that has 
              not been described yet, use this tool to determine what is relevant.

            ### Input Format:
            Do not use markdown! 
            The tool requires a search query string, where you can inquire about the scenario module.
            Additionally, if relevant, you can provide the name of the node from the graph relating to the location that you 
            wish to inquire about. That could be the node where the player currently is if you want to learn
            more about the location, or the node where the player has been previously if you need important details 
            about a location that the player has already visited. If you inquire about a node that the player has yet 
            to discover, be very careful to avoid revealing any details that the player has not yet encountered.
            The input to this tool must be in the following RAW JSON format, where the "nodename" property is optional:
            {{
                "query": "<The search query string>",
                "nodename": "<The name of the node in the graph>",
            }}

            ### Example Uses:

            #### Scenario 1 – Player in a Castle Hall
            **Player Input:** 'I want to talk to the ghost of the former king.'

            You are unsure if a ghost exists in the castle hall. You call the tool with the input  
            {{
                "query": "Is there a ghost of the former king in the castle hall?",
                "nodename": "Castle Hall",
            }}

            You retrieve details, learning that there is a suit of armor containing red eyes that greets the player as Ulemar, 
            the Knight of the King.

            #### Scenario 2 – Exploring a Village
            **Player Input:** 'I enter a random house in the village. What do I see?'

            You are unsure about the houses and call the tool with the input  
            {{
                "query": "Tell me about the houses in the village.",
                "nodename": "Village Square",
            }}

            If the adventure module contains details about the house, the tool retrieves them.  
            If the house is not mentioned, you may improvise a minor detail (e.g.,  
            'A modest home with a fireplace and a wooden table') while ensuring it does not contradict existing world details.

            #### Scenario 3 – Deviating from the Main Story
            **Player Input:** 'I leave the dungeon and wander into the forest.'

            You are unsure what the player can find in the forest. Since you are searching for general information 
            about an area that does not correlate with a specific node, you call the tool with the input  
            {{
                "query": "Tell me about the forest. Are there any objectives there? Does anything happen when the player leaves the dungeon?",
            }}

            If the adventure module has no information about the forest, you may allow limited exploration but  
            eventually use the tool to reference details from the current chapter, nudging the player back toward  
            the dungeon in a natural way.

            ---

            Use this tool as often as needed to maintain consistency, but allow for creative flexibility when small  
            details are missing. Never fabricate major lore elements if the adventure module provides context. You  
            can call this tool multiple times in a single narrative to ensure the story remains coherent and engaging.
            """);
        tools.Add(searchScenarioTool);

        var updateGraphTool = new UpdateGraphTool(
            _configuration,
            campaign,
            playerInput,
            "updategraphtool",
            """
            This tool evaluates whether the player can advance along an edge in the **narrative graph** 
            by checking the conditions between two given nodes. If all conditions are met, the tool updates the graph accordingly.

            ### Conditions for Use:
            - There must be an **edge** between the two nodes.
            - The **source node** must be marked as **ongoing** or **completed**.
            - The **target node** must be **undiscovered**.

            ### Use Cases:
            - When the agent suspects the player **may be able to progress** in the adventure.
            - Before updating the graph, to **validate whether all edge conditions are fulfilled**.
            - If traversal is **allowed**, the tool will:
              - Mark the **source node** as **completed**.
              - Set the **target node** as **ongoing**.
              - Flag the **edge** as **visited**.

            ---

            ### **Expected Input Format:**
            Input must be provided in **RAW JSON format** (do not use markdown):
            {{
                ""sourcenodename"": ""<name of the source node>"",
                ""targetnodename"": ""<name of the target node>""
            }}

            - **sourcenodename**: The current location or plot point the player is at.
            - **targetnodename**: The next location or plot point the player wants to reach.

            ---

            ### **Tool Output:**
            #### **If traversal is NOT allowed:**
            - The tool returns a string listing each **edge condition** and its **evaluation result**.
            
              **Example:**
              `"condition_1: true condition_2: false"`

            - If any condition is **false**, traversal is not yet possible, and the agent should **not** update the graph.

            #### **If all conditions are met:**
            - The tool **updates the graph** and confirms the update by showing the modified graph.

            ---

            ### **Example 1: Advancement Allowed**
            #### **Scenario:**
            The player is exploring an **Ancient Crypt**, attempting to access a **Hidden Chamber** requiring a special key.

            #### **Game Interaction:**
            - **Player:**  
            _"I insert the Ornate Crypt Key into the lock and push the heavy stone door open."_

            - **DM:**  
            _"With a deep, grinding noise, the stone door slides aside, revealing a darkened chamber beyond.  
            The air is thick with the scent of dust and decay, and you can just make out the shapes of sarcophagi  
            lining the walls. The passage ahead is now open to you."_

            #### **Agent Decision:**
            The player has used the required key, fulfilling the condition for entering the **Hidden Chamber**.  
            The agent calls the tool with:
            {{
                ""sourcenodename"": ""Ancient Crypt Entrance"",
                ""targetnodename"": ""Hidden Chamber""
            }}

            #### **Tool Output:**
            The graph updates successfully:
            A fully updated graph is returned because:
            `"Has the player used the Ornate Crypt Key?: true"`

            - **Source node**: _Completed_
            - **Target node**: _Ongoing_

            ---

            ### **Example 2: Advancement Blocked**
            #### **Scenario:**
            The player is at a **Ruined Bridge**, attempting to cross, but the game requires them to **reinforce the structure** first.

            #### **Game Interaction:**
            - **Player:**  
              _"I step onto the old bridge, carefully testing its weight as I make my way across."_

            - **DM:**  
              _"The wooden planks creak and shift beneath your feet. Halfway across, you hear a loud snap as  
              one of the supports gives way! You barely manage to scramble back to safety as the bridge  
              shudders ominously. It doesn't look stable enough to cross."_

            #### **Agent Decision:**
            The player has **not yet reinforced** the bridge, meaning traversal is **not possible**.  
            The agent calls the tool with:
            {{
                ""sourcenodename"": ""Ruined Bridge"",
                ""targetnodename"": ""Other Side of the Chasm""
            }}

            #### **Tool Output:**
            `"Has the player defeated the guardian of the bridge?: true Has the player reinforced the bridge with sturdy materials?: false"`

            Since **not all conditions are met**, the graph **is NOT updated**.
            """);
        tools.Add(updateGraphTool);

        return tools;
    }
}
