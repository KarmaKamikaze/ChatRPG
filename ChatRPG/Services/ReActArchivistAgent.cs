using System.Text;
using ChatRPG.API;
using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;
using Message = ChatRPG.Data.Models.Message;
using MessageRole = ChatRPG.Data.Models.MessageRole;

namespace ChatRPG.Services;

public class ReActArchivistAgent
{
    private readonly OpenAiProvider _provider;
    private readonly IPersistenceService _persistenceService;
    private readonly IConfiguration _configuration;
    private readonly bool _archivistDebugMode;
    private readonly bool _summarizeMessages;

    public ReActArchivistAgent(IConfiguration configuration, IPersistenceService persistenceService)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        _provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _archivistDebugMode = configuration.GetValue<bool>("ArchivistChainDebug");
        _summarizeMessages = configuration.GetValue<bool>("ShouldSummarize");
        _persistenceService = persistenceService;
        _configuration = configuration;
    }

    public async Task SaveCurrentState(Campaign campaign)
    {
        await _persistenceService.SaveAsync(campaign);
    }

    public async Task UpdateCampaignFromNarrative(Campaign campaign, string input, string narrative)
    {
        var characters = new StringBuilder();
        characters.Append("{\"characters\": [\n");

        foreach (var character in campaign.Characters)
        {
            characters.Append(
                $"{{\"name\": \"{character.Name}\", \"description\": \"{character.Description}\", \"type\": \"{character.Type}\"}},");
        }

        characters.Length--; // Remove last comma
        characters.Append("\n]}");

        var environments = new StringBuilder();
        environments.Append("{\"environments\": [\n");

        foreach (var environment in campaign.Environments)
        {
            environments.Append(
                $"{{\"name:\" \"{environment.Name}\", \"description\": \"{environment.Description}\"}},");
        }

        environments.Length--; // Remove last comma
        environments.Append("\n]}");

        var agent = SelectAgent(campaign, characters.ToString(), environments.ToString());

        var tools = CreateTools(campaign, input, narrative);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var newInformation = $"The player says: {input}\nThe DM says: {narrative}";

        var chain = Set(newInformation, "input") | agent;
        await chain.RunAsync("text");
    }

    private List<AgentTool> CreateTools(Campaign campaign, string input, string response)
    {
        var tools = new List<AgentTool>();

        var updateCharacterToolDescription = new StringBuilder();

        updateCharacterToolDescription.Append("""
                                              ### Character Creation & Update Tool

                                              This tool is used to **create a new character** or **update an existing character** in the campaign.
                                              A character can be an ally, enemy, neutral figure, or even a mysterious unknown.

                                              ---

                                              ### Conditions for Use:
                                              - The narrative introduces a **new character**.
                                              - The narrative updates information about an **existing character**.

                                              ---

                                              ### Use Cases:
                                              - A new NPC or creature is named or described.
                                              - The narrative updates a character's **appearance**, **health**, or **role**.
                                              - The player interacts with someone important enough to track.

                                              ---

                                              ### Character Description Guidelines:
                                              - Include **physical features**, **personality**, or **distinctive traits**.
                                              - Mention **known affiliations**, **roles**, or **notable actions**.
                                              - Keep descriptions vivid and interesting for the player.

                                              ---

                                              ### Expected Input Format:
                                              Input must be provided in **RAW JSON format** (do not use markdown):

                                              {{
                                                "name": "<character name>",
                                                "description": "<new or updated character description>",
                                                "type": "<character type>",
                                                "state": "<character health state>"
                                              }}

                                              - **name**: The character's name.
                                              - **description**: A detailed and engaging character description.
                                              - **type**: One of the following values: {{
                                              """);

        var characterTypes = Enum.GetNames<CharacterType>();
        for (var i = 0; i < characterTypes.Length; i++)
        {
            updateCharacterToolDescription.Append(characterTypes[i]);
            if (i < characterTypes.Length - 1)
            {
                updateCharacterToolDescription.Append(", ");
            }
        }

        updateCharacterToolDescription.Append("""
                                              }}
                                              - **state**: One of the following values: {{Dead, Unconscious, HeavilyWounded, LightlyWounded, Healthy}}

                                              ---

                                              The tool should only be used **once per character**.
                                              """);

        var updateCharacterTool =
            new UpdateCharacterTool(campaign, "updatecharactertool", updateCharacterToolDescription.ToString());
        tools.Add(updateCharacterTool);

        var updateEnvironmentTool = new UpdateEnvironmentTool(
            campaign,
            "updateenvironmenttool",
            """
            This tool must be used to **create a new environment** or **update an existing environment** in the campaign.  

            **Example Usage:**  
            The narrative text mentions a new environment or contains changes to an existing environment.

            ---

            ### **What is an Environment?**
            An environment refers to a **place**, **location**, or **area** that is well enough defined to warrant its own description.  
            Such places could include:
            - A **landmark** with its own history.
            - A **building** where story events take place.
            - A larger place like a **magical forest**.

            ---

            ### **Tool Input Format**
            Input to this tool must be in the following **RAW JSON format**:
            {{
                "name": "<environment name>",
                "description": "<new or updated environment description>",
                "isPlayerHere": <true if the Player character is currently at this environment, false otherwise>
            }}

            ### **Description of an Environment**
            - The **description** could cover:
              - Its **physical characteristics**.
              - Its **significance** in the story.
              - The **creatures** that inhabit it.
              - The **weather** or other descriptive features.

            The goal is to provide the Player with **useful information** about the places they travel to, 
            while keeping the locations' descriptions **interesting**, **mysterious**, and **engaging**.

            ---

            ### **Important Notes**
            - The tool should **only be used once** per environment to avoid redundancy and maintain clarity in the narrative.
            """);
        tools.Add(updateEnvironmentTool);

        var updateGraphTool = new UpdateGraphTool(
            _configuration,
            campaign,
            input,
            response,
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
                ""sourcenodename"": ""<name of the source nod>"",
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

    public async Task StoreMessagesInCampaign(Campaign campaign, string playerInput, string assistantOutput)
    {
        var newMessages = new List<LangChain.Providers.Message>
        {
            new(playerInput.Trim(), LangChain.Providers.MessageRole.Human),
            new(assistantOutput.Trim(), LangChain.Providers.MessageRole.Ai)
        };

        var summaryLlm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.4 }
        };

        if (_summarizeMessages)
        {
            campaign.GameSummary = await summaryLlm.SummarizeAsync(newMessages, campaign.GameSummary);
        }
        else
        {
            campaign.GameSummary += string.Join("\n", newMessages.Select(m => m.Content)) + "\n";
        }

        foreach (var message in newMessages)
        {
            // Only add the message, if the list is empty.
            // This is because if the list is empty, the input is the initial prompt. Not player input.
            if (campaign.Messages.Count == 0 && message.Role == LangChain.Providers.MessageRole.Human)
            {
                continue;
            }

            campaign.Messages.Add(new Message(campaign,
                (message.Role == LangChain.Providers.MessageRole.Human ? MessageRole.User : MessageRole.Assistant),
                message.Content.Trim()));
        }
    }

    /// <summary>
    /// Selects the appropriate ReActAgentChain based on if the campaign utilizes a NarrativeGraph.
    /// </summary>
    /// <param name="campaign">The campaign being played.</param>
    /// <param name="characters">A JSON string describing the characters present in the campaign.</param>
    /// <param name="environments">A JSON string describing the environments in the campaign.</param>
    /// <returns>An agent that utilizes a NarrativeGraph if necessary.</returns>
    private ReActAgentChain SelectAgent(Campaign campaign, string characters, string environments)
    {
        var llm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.7 }
        };

        // Create the ReActAgentChain with the campaign's NarrativeGraph if it exists, meaning the game is running in
        // pre-defined scenarios. Otherwise, create the agent without the NarrativeGraph for open-world.
        if (campaign.NarrativeGraph != null)
        {
            ArgumentException.ThrowIfNullOrEmpty(_configuration.GetSection("SystemPrompts")
                .GetValue<string>("ArchivistWithGraphReActPrompt"));
            var reActPrompt = _configuration.GetSection("SystemPrompts")
                .GetValue<string>("ArchivistWithGraphReActPrompt")!;
            return new ReActAgentChain(_archivistDebugMode ? llm.UseConsoleForDebug() : llm, campaign.NarrativeGraph,
                characters: characters, campaign.Player.Name, environments, campaign.GameSummary, reActPrompt);
        }
        else
        {
            ArgumentException.ThrowIfNullOrEmpty(_configuration.GetSection("SystemPrompts")
                .GetValue<string>("ArchivistReActPrompt"));
            var reActPrompt = _configuration.GetSection("SystemPrompts").GetValue<string>("ArchivistReActPrompt")!;
            return new ReActAgentChain(_archivistDebugMode ? llm.UseConsoleForDebug() : llm, characters: characters,
                campaign.Player.Name, environments, campaign.GameSummary, reActPrompt);
        }
    }
}
