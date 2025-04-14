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
    private readonly Gpt4OmniModel _llm;
    private readonly bool _examinerDebugMode;
    private readonly bool _shouldIncludePreviousMessages;
    private readonly string _reactPrompt;


    public ReActExaminerAgent(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("ExaminerReActPrompt"));
        _configuration = configuration;
        var provider = new OpenAiProvider(_configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.4 }
        };
        _reactPrompt = _configuration.GetSection("SystemPrompts").GetValue<string>("ExaminerReActPrompt")!;
        _examinerDebugMode = _configuration.GetValue<bool>("ExaminerChainDebug");
        _shouldIncludePreviousMessages = _configuration.GetValue<bool>("ShouldSummarize");
    }


    public async Task<string> ExaminePlayerInput(Campaign campaign, string playerInput)
    {
        var agent = new ReActAgentChain(_examinerDebugMode ? _llm.UseConsoleForDebug() : _llm,
            reActPrompt: _reactPrompt,
            gameSummary: ToolUtilities.ConstructSummary(campaign, _shouldIncludePreviousMessages),
            graph: campaign.NarrativeGraph);

        var tools = await CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var chain = Set(playerInput, "input") | agent;

        return (await chain.RunAsync("text"))!;
    }

    private async Task<List<AgentTool>> CreateTools(Campaign campaign)
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

        return tools;
    }
}
