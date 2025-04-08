using ChatRPG.API;
using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.Services;

public class ReActNarratorAgent : IReActLlmClient
{
    private readonly IConfiguration _configuration;
    private readonly OpenAiProvider _provider;
    private readonly bool _narratorDebugMode;
    private readonly bool _shouldIncludePreviousMessages;

    public ReActNarratorAgent(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        _configuration = configuration;
        _provider = new OpenAiProvider(_configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _narratorDebugMode = _configuration.GetValue<bool>("NarrativeChainDebug");
        _shouldIncludePreviousMessages = _configuration.GetValue<bool>("ShouldSummarize");
    }

    public async Task<string> GetChatCompletionAsync(Campaign campaign, string actionPrompt, string input)
    {
        var llm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.7 }
        };

        var agent = SelectAgent(campaign, llm, actionPrompt);

        var tools = await CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var chain = Set(input, "input") | agent;
        return (await chain.RunAsync("text"))!;
    }

    public async IAsyncEnumerable<string> GetStreamedChatCompletionAsync(Campaign campaign, string actionPrompt,
        string input)
    {
        var llm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = true, Temperature = 0.7 }
        };

        var eventProcessor = new LlmEventProcessor(llm);
        var agent = SelectAgent(campaign, llm, actionPrompt);
        var tools = await CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var chain = Set(input, "input") | agent;

        var response = chain.RunAsync("text");

        await foreach (var content in eventProcessor.GetContentStreamAsync())
        {
            yield return content;
        }

        await response;
    }

    private async Task<List<AgentTool>> CreateTools(Campaign campaign)
    {
        var tools = new List<AgentTool>();
        var utils = new ToolUtilities(_configuration);

        var woundCharacterTool = new WoundCharacterTool(
            _configuration.GetSection("SystemPrompts").GetValue<string>("WoundCharacterInstruction")!,
            campaign,
            utils,
            "woundcharactertool",
            """
            This tool must be used when a character is hurt or wounded as a result of **unnoticed attacks** 
            or performing **dangerous activities** that lead to injury. 

            ### Conditions for Use:
            - The **damage cannot be mitigated, dodged, or avoided**.
            - The character is **not engaged in active battle**.

            ### Example Scenarios:
            - **Unnoticed Attack:**  
              - A character **performs a sneak attack** without being spotted by their enemies.  

            - **Dangerous Activity:**  
              - A character **threatens a King**, causing his guards to intervene violently.  
              - A reckless action leads to **accidental harm** (e.g., triggering a trap).  

            ### Input Format:
            Input to this tool must be provided in **RAW JSON format** (do not use markdown):
            {
                "input": "<The player's input>",
                "severity": "<Describes how devastating the injury is based on the action>"
            }

            ### Accepted Values:
            - **`severity` values:** `{low, medium, high, extraordinary}`

            This tool should be used **only once per character at most**, and only when they are **not in battle**.
            """);
        tools.Add(woundCharacterTool);

        var healCharacterTool = new HealCharacterTool(
            _configuration.GetSection("SystemPrompts").GetValue<string>("HealCharacterInstruction")!,
            campaign,
            utils,
            "healcharactertool",
            """
            This tool must be used when a character performs an action that could heal or restore them to 
            health after being wounded. The tool is only appropriate if the healing can be done without any 
            further actions.

            ### Example Usage:
            - **Scenario 1: Healing After an Attack**  
              - A character is wounded by an enemy attack.  
              - The player decides to **heal the character**.  

            - **Scenario 2: Healing via Items or Environment**  
              - A character **consumes a beneficial item** such as a potion or a magical artifact.  
              - The character **spends time in an area** that provides healing benefits.  
              - Resting may provide **modest healing effects**, depending on the duration of the rest.  

            ### Input Format:
            Input to this tool must be in the following **RAW JSON format** (do not use markdown):
            {
                "input": "<The player's input>",
                "magnitude": "<Describes how much health the character will regain based on the action>"
            }

            ### Accepted Values:
            - **`magnitude` values:** `{low, medium, high, extraordinary}`

            This tool should be used **only once per character at most**.
            """);
        tools.Add(healCharacterTool);

        var battleTool = new BattleTool(
            _configuration.GetSection("SystemPrompts").GetValue<string>("BattleInstruction")!,
            campaign,
            utils,
            "battletool",
            """
            Use the battle tool to resolve battle or combat between two participants. A participant is 
            a single character and cannot be a combination of characters. If there are more 
            than two participants, the tool must be used once per attacker to give everyone a chance at fighting. 

            The battle tool will give each participant a chance to fight the other participant. The tool should 
            also be used when an attack can be mitigated or dodged by the involved participants. It is also 
            possible for either or both participants to miss. A hit chance specifier will help adjust the chance 
            that a participant gets to retaliate.

            ### Example Usage:
            - **Scenario 1: Two Combatants**  
              - There are only two combatants.  
              - Call the tool **only ONCE**, since both characters get an attack.  

            - **Scenario 2: Three Combatants (Player vs. Two Assassins)**  
              - The battle tool is called first with the Player's character as **participant one**  
                and one of the assassins as **participant two**.  
              - The Player has a high chance of hitting the assassin.  
              - The assassins must be precise, making their hits harder to land, but they deal high damage when successful.  
              - If **participant one hits participant two** and **participant two misses participant one**,  
                this round is resolved.  
              - The tool is then called **again** with the Player’s character as participant one and the other assassin as participant two.  
              - Since participant one has already hit once in this battle, a **penalty is imposed** on their hit chance,  
                which accumulates for each successful attack in the battle.  

            ### Damage Severity:
            - The **damage severity** describes how powerful an attack is, derived from the narrative description.
            - If participants engage in a friendly sparring fight, do not intend to hurt, or are in a mock battle,  
              the **damage severity is `<harmless>`**.
            - If no direct description is available, estimate the impact of an attack based on the **character type**  
              and their **description**.

            ### Input Format:
            Input to this tool must be in the following **RAW JSON format** (do not use markdown):
            {
                "participant1": {
                    "name": "<name of participant one>",
                    "description": "<description of participant one>"
                },
                "participant2": {
                    "name": "<name of participant two>",
                    "description": "<description of participant two>"
                },
                "participant1HitChance": "<hit chance specifier for participant one>",
                "participant2HitChance": "<hit chance specifier for participant two>",
                "participant1DamageSeverity": "<damage severity for participant one>",
                "participant2DamageSeverity": "<damage severity for participant two>"
            }

            ### Accepted Values:
            - **`participant#HitChance` specifiers:** `{high, medium, low, impossible}`
            - **`participant#DamageSeverity` values:** `{harmless, low, medium, high, extraordinary}`

            The narrative battle **ends** when each character has had the chance to attack another 
            character **at most once**.
            """);
        tools.Add(battleTool);

        if (!campaign.IsOpenWorld)
        {
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
                {
                    "query": "<The search query string>",
                    "nodename": "<The name of the node in the graph>",
                }

                ### Example Uses:

                #### Scenario 1 – Player in a Castle Hall
                **Player Input:** 'I want to talk to the ghost of the former king.'

                You are unsure if a ghost exists in the castle hall. You call the tool with the input  
                {
                    "query": "Is there a ghost of the former king in the castle hall?",
                    "nodename": "Castle Hall",
                }

                You retrieve details, learning that there is a suit of armor containing red eyes that greets the player as Ulemar, 
                the Knight of the King.

                #### Scenario 2 – Exploring a Village
                **Player Input:** 'I enter a random house in the village. What do I see?'

                You are unsure about the houses and call the tool with the input  
                {
                    "query": "Tell me about the houses in the village.",
                    "nodename": "Village Square",
                }

                If the adventure module contains details about the house, the tool retrieves them.  
                If the house is not mentioned, you may improvise a minor detail (e.g.,  
                'A modest home with a fireplace and a wooden table') while ensuring it does not contradict existing world details.

                #### Scenario 3 – Deviating from the Main Story
                **Player Input:** 'I leave the dungeon and wander into the forest.'

                You are unsure what the player can find in the forest. Since you are searching for general information 
                about an area that does not correlate with a specific node, you call the tool with the input  
                {
                    "query": "Tell me about the forest. Are there any objectives there? Does anything happen when the player leaves the dungeon?",
                }

                If the adventure module has no information about the forest, you may allow limited exploration but  
                eventually use the tool to reference details from the current chapter, nudging the player back toward  
                the dungeon in a natural way.

                ---

                Use this tool as often as needed to maintain consistency, but allow for creative flexibility when small  
                details are missing. Never fabricate major lore elements if the adventure module provides context. You  
                can call this tool multiple times in a single narrative to ensure the story remains coherent and engaging.
                """);
            tools.Add(searchScenarioTool);
        }

        return tools;
    }

    /// <summary>
    /// Selects the appropriate ReActAgentChain based on if the campaign utilizes a NarrativeGraph.
    /// </summary>
    /// <param name="campaign">The campaign being played.</param>
    /// <param name="llm">The OpenAI LLM model.</param>
    /// <param name="actionPrompt">A specific prompt based on the action mode selected by the player.</param>
    /// <returns>An agent that utilizes a NarrativeGraph if necessary.</returns>
    private ReActAgentChain SelectAgent(Campaign campaign, Gpt4OmniModel llm, string actionPrompt)
    {
        // Create the ReActAgentChain with the campaign's NarrativeGraph if it exists, meaning the game is running in
        // pre-defined scenarios. Otherwise, create the agent without the NarrativeGraph for open-world.
        if (!campaign.IsOpenWorld)
        {
            ArgumentException.ThrowIfNullOrEmpty(_configuration.GetSection("SystemPrompts")
                .GetValue<string>("NarratorWithGraphReActPrompt"));
            var reActPrompt = _configuration.GetSection("SystemPrompts")
                .GetValue<string>("NarratorWithGraphReActPrompt")!;
            return new ReActAgentChain(_narratorDebugMode ? llm.UseConsoleForDebug() : llm, reactPrompt: reActPrompt,
                gameSummary: ToolUtilities.ConstructSummary(campaign, _shouldIncludePreviousMessages),
                graph: campaign.NarrativeGraph, actionPrompt: actionPrompt);
        }
        else
        {
            ArgumentException.ThrowIfNullOrEmpty(_configuration.GetSection("SystemPrompts")
                .GetValue<string>("NarratorReActPrompt"));
            var reActPrompt = _configuration.GetSection("SystemPrompts").GetValue<string>("NarratorReActPrompt")!;
            return new ReActAgentChain(_narratorDebugMode ? llm.UseConsoleForDebug() : llm, reactPrompt: reActPrompt,
                gameSummary: ToolUtilities.ConstructSummary(campaign, _shouldIncludePreviousMessages),
                actionPrompt: actionPrompt);
        }
    }
}
