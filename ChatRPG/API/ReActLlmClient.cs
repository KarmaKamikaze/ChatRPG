using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.API;

public class ReActLlmClient : IReActLlmClient
{
    private readonly IConfiguration _configuration;
    private readonly OpenAiProvider _provider;
    private readonly string _reActPrompt;
    private readonly bool _narratorDebugMode;

    public ReActLlmClient(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts").GetValue<string>("ReAct"));
        _configuration = configuration;
        _reActPrompt = _configuration.GetSection("SystemPrompts").GetValue<string>("ReAct")!;
        _provider = new OpenAiProvider(_configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _narratorDebugMode = _configuration.GetValue<bool>("NarrativeChainDebug");
    }

    public async Task<string> GetChatCompletionAsync(Campaign campaign, string actionPrompt, string input)
    {
        var llm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.7 }
        };

        var agent = new ReActAgentChain(_narratorDebugMode ? llm.UseConsoleForDebug() : llm, _reActPrompt,
            actionPrompt: actionPrompt, campaign.GameSummary);
        var tools = CreateTools(campaign);
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
        var agent = new ReActAgentChain(_narratorDebugMode ? llm.UseConsoleForDebug() : llm, _reActPrompt,
            actionPrompt: actionPrompt, campaign.GameSummary);
        var tools = CreateTools(campaign);
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

    private List<AgentTool> CreateTools(Campaign campaign)
    {
        var tools = new List<AgentTool>();
        var utils = new ToolUtilities(_configuration);

        var woundCharacterTool = new WoundCharacterTool(_configuration, campaign, utils, "woundcharactertool",
            "This tool must be used when a character will be hurt or wounded resulting from unnoticed attacks" +
            "or performing dangerous activities that will lead to injury. The tool is only appropriate if the damage " +
            "cannot be mitigated, dodged, or avoided. Example: A character performs a sneak attack " +
            "without being spotted by the enemies they try to attack. A dangerous activity could be to threateningly " +
            "approach a King, which may result in injury when his guards step forward to stop the character. " +
            "Input to this tool must be in the following RAW JSON format: {\"input\": \"The player's input\", " +
            "\"severity\": \"Describes how devastating the injury to the character will be based on the action. " +
            "Can be one of the following values: {low, medium, high, extraordinary}}\". Do not use markdown, " +
            "only raw JSON as input. Use this tool only once per character at most and only if they are not engaged " +
            "in battle.");
        tools.Add(woundCharacterTool);

        var healCharacterTool = new HealCharacterTool(_configuration, campaign, utils, "healcharactertool",
            "This tool must be used when a character performs an action that could heal or restore them to " +
            "health after being wounded. The tool is only appropriate if the healing can be done without any " +
            "further actions. Example: A character is wounded by an enemy attack and the player decides to heal " +
            "the character. Another example would be a scenario where a character consumes a beneficial item like " +
            "a potion, a magical item, or spends time in an area that could provide healing " +
            "benefits. Resting may provide modest healing effects depending on the duration of the rest. " +
            "Input to this tool must be in the following RAW JSON format: {\"input\": \"The player's input\", " +
            "\"magnitude\": \"Describes how much health the character will regain based on the action. " +
            "Can be one of the following values: {low, medium, high, extraordinary}}\". " +
            "Do not use markdown, only raw JSON as input. Use this tool only once per character at most.");
        tools.Add(healCharacterTool);

        var battleTool = new BattleTool(_configuration, campaign, utils, "battletool",
            "Use the battle tool to resolve battle or combat between two participants. A participant is " +
            "a single character and cannot be a combination of characters. If there are more " +
            "than two participants, the tool must be used once per attacker to give everyone a chance at fighting. " +
            "The battle tool will give each participant a chance to fight the other participant. The tool should " +
            "also be used when an attack can be mitigated or dodged by the involved participants. It is also " +
            "possible for either or both participants to miss. A hit chance specifier will help adjust the chance " +
            "that a participant gets to retaliate. Example: There are only two combatants. Call the tool only ONCE " +
            "since both characters get an attack. Another example: There are three combatants, the Player's character " +
            "and two assassins. The battle tool is called first with the Player's character as participant one and " +
            "one of the assassins as participant two. Chances are high that the player will hit the assassin but " +
            "assassins must be precise, making it harder to hit, however, they deal high damage if they hit. We " +
            "observe that the participant one hits participant two and participant two misses participant one. " +
            "After this round of battle has been resolved, call the tool again with the Player's character as " +
            "participant one and the other assassin as participant two. Since participant one in this case has " +
            "already hit once during this narrative, we impose a penalty to their hit chance, which is " +
            "accumulative for each time they hit an enemy during battle. The damage severity describes how " +
            "powerful the attack is which is derived from the narrative description of the attacks. " +
            "If the participants engage in a friendly sparring fight, does not intend to hurt, or does mock battle, " +
            "the damage severity is <harmless>. " +
            "If there are no direct description, estimate the impact of an attack based on the character type and " +
            "their description. Input to this tool must be in the following RAW JSON format: {\"participant1\": " +
            "{\"name\": \"<name of participant one>\", \"description\": \"<description of participant one>\"}, " +
            "\"participant2\": {\"name\": \"<name of participant two>\", \"description\": " +
            "\"<description of participant two>\"}, \"participant1HitChance\": \"<hit chance specifier for " +
            "participant one>\", \"participant2HitChance\": \"<hit chance specifier for participant two>\", " +
            "\"participant1DamageSeverity\": \"<damage severity for participant one>\", " +
            "\"participant2DamageSeverity\": \"<damage severity for participant two>\"} where participant#HitChance " +
            "specifiers are one of the following {high, medium, low, impossible} and participant#DamageSeverity is " +
            "one of the following {harmless, low, medium, high, extraordinary}. Do not use markdown, only raw JSON as " +
            "input. The narrative battle is over when each character has had the chance to attack another character at " +
            "most once.");
        tools.Add(battleTool);

        var searchScenarioTool = new SearchScenarioTool(_configuration, campaign, "searchscenariotool",
            "This tool must be used whenever you are unsure of what is available to the player in the " +
            "current location, uncertain about what should happen next, or need to reference existing details from " +
            "the adventure module to maintain consistency. The tool helps you retrieve structured information " +
            "about the game world, ensuring it adheres to the story's established details while still allowing for " +
            "player agency and exploration.\n" +
            "When to use this tool:\n" +
            "- Unknown Details: If you do not have enough information about a location, NPC, quest, faction, or " +
            "available actions, this tool must be used to find relevant context from the adventure module.\n" +
            "- Player Agency & Story Consistency: You should follow the scenario structure but can adapt if minor " +
            "details are missing. However, if key details exist in the adventure module, they must be used to shape " +
            "the game world.\n" +
            "- Keeping the Player on Track: If the player strays too far from the main story while exploring an area, " +
            "the tool can be used to find details that naturally guide them back into the intended narrative without " +
            "restricting their choices.\n" +
            "- Exploration & Interaction: If the player takes an action related to an NPC, object, or location that has " +
            "not been described yet, use this tool to determine what is relevant.\n" +
            "Example Uses:\n" +
            "Scenario 1 – Player in a Castle Hall\n" +
            "Player Input: \"I want to talk to the ghost of the former king.\"\n" +
            "You are unsure if a ghost exists in the castle hall. You call the tool with the input \"Is there a ghost " +
            "of the former king in the castle hall?\" and retrieve details, learning " +
            "that there is a suit of armor containing red eyes that greets the player as Ulemar, the Knight of the King.\n" +
            "Scenario 2 – Exploring a Village\n" +
            "Player Input: \"I enter a random house in the village. What do I see?\"\n" +
            "You are unsure about the houses and call the tool with the input \"Tell me about the houses in the village.\"\n" +
            "If the adventure module contains details about the house, the tool retrieves them.\n" +
            "If the house is not mentioned, you may improvise a minor detail (e.g., \"A modest home with a " +
            "fireplace and a wooden table\") while ensuring it does not contradict existing world details.\n" +
            "Scenario 3 – Deviating from the Main Story\n" +
            "Player Input: \"I leave the dungeon and wander into the forest.\"\n" +
            "You are unsure what the player can find in the forest. You call the tool with the input \"Tell me about " +
            "the forest. Are there any objectives there? Does anything happen when the player leaves the dungeon?\"\n" +
            "If the adventure module has no information about the forest, you may allow limited exploration but " +
            "eventually use the tool to reference details from the current chapter, nudging the player back toward " +
            "the dungeon in a natural way.\n" +
            "Use this tool as often as needed to maintain consistency, but allow for creative flexibility when small " +
            "details are missing. Never fabricate major lore elements if the adventure module provides context. You " +
            "can call this tool multiple times in a single narrative to ensure the story remains coherent and engaging.");
        tools.Add(searchScenarioTool);

        return tools;
    }
}
