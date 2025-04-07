using System.Globalization;
using ChatRPG.Data.Models;
using LangChain.Abstractions.Schema;
using LangChain.Chains.HelperChains;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Chains.StackableChains.ReAct;
using LangChain.Memory;
using LangChain.Providers;
using LangChain.Schema;
using static LangChain.Chains.Chain;
using Message = LangChain.Providers.Message;
using MessageRole = LangChain.Providers.MessageRole;

namespace ChatRPG.API;

public sealed class ReActAgentChain : BaseStackableChain
{
    private const string ReActAnswer = "answer";
    private readonly ConversationBufferMemory _conversationBufferMemory;
    private readonly IChatModel _model;
    private readonly int _maxActions;
    private readonly string _reActPrompt;
    private readonly Dictionary<string, AgentTool> _tools = new();
    private StackChain? _chain;

    private bool _useCache;
    private string _userInput = string.Empty;

    private readonly string _actionPrompt;
    private readonly string? _gameSummary;
    private readonly string _playerCharacter;
    private readonly string _characters;
    private readonly string _environments;
    private readonly string _narrativeGraph;
    private readonly string _graphExtensionSummary;

    public string DefaultPrompt = """
                                  Assistant is a large language model trained by OpenAI.

                                  Assistant is designed to be able to assist with a wide range of tasks, from answering simple questions to providing in-depth explanations and discussions on a wide range of topics. As a language model, Assistant is able to generate human-like text based on the input it receives, allowing it to engage in natural-sounding conversations and provide responses that are coherent and relevant to the topic at hand.

                                  Assistant is constantly learning and improving, and its capabilities are constantly evolving. It is able to process and understand large amounts of text, and can use this knowledge to provide accurate and informative responses to a wide range of questions. Additionally, Assistant is able to generate its own text based on the input it receives, allowing it to engage in discussions and provide explanations and descriptions on a wide range of topics.

                                  Overall, Assistant is a powerful tool that can help with a wide range of tasks and provide valuable insights and information on a wide range of topics. Whether you need help with a specific question or just want to have a conversation about a particular topic, Assistant is here to assist.

                                  TOOLS:
                                  ------

                                  Assistant has access to the following tools:

                                  {tools}

                                  To use a tool, please use the following format:

                                  ```
                                  Thought: Do I need to use a tool? Yes
                                  Action: the action to take, should be one of [{tool_names}]
                                  Action Input: the input to the action
                                  Observation: the result of the action
                                  ```

                                  When you have a response to say to the Human, or if you do not need to use a tool, you MUST use the format:

                                  ```
                                  Thought: Do I need to use a tool? No
                                  Final Answer: [your response here]
                                  ```

                                  Always add [END] after final answer

                                  Special action:
                                  {action}

                                  Begin!

                                  Game summary:
                                  {summary}

                                  Previous conversation history:
                                  {history}

                                  New input: {input}
                                  """;

    public ReActAgentChain(
        IChatModel model,
        string? reactPrompt = null,
        string? gameSummary = null,
        NarrativeGraph? graph = null,
        string? graphExtensionSummary = null,
        string? actionPrompt = null,
        string? characters = null,
        string? playerCharacter = null,
        string? environments = null,
        string inputKey = "input",
        string outputKey = "text",
        int maxActions = 20)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _model.Settings!.StopSequences = ["Observation", "[END]"];
        _reActPrompt = reactPrompt ?? DefaultPrompt;
        _maxActions = maxActions;

        InputKeys = [inputKey];
        OutputKeys = [outputKey];

        _gameSummary = gameSummary;
        _narrativeGraph = graph?.Serialize() ?? string.Empty;
        _graphExtensionSummary = graphExtensionSummary ?? string.Empty;
        _actionPrompt = actionPrompt ?? string.Empty;
        _characters = characters ?? string.Empty;
        _playerCharacter = playerCharacter ?? string.Empty;
        _environments = environments ?? string.Empty;

        var messageFormatter = new MessageFormatter
        {
            AiPrefix = "",
            HumanPrefix = "",
            SystemPrefix = ""
        };

        var chatMessageHistory = new ChatMessageHistory()
        {
            // Do not save human messages
            IsMessageAccepted = x => (x.Role != MessageRole.Human)
        };

        _conversationBufferMemory = new ConversationBufferMemory(chatMessageHistory)
        {
            Formatter = messageFormatter
        };
    }

    private void InitializeChain()
    {
        var toolNames = string.Join(",", _tools.Select(x => x.Key));
        var tools = string.Join("\n", _tools.Select(x => $"{x.Value.Name}, {x.Value.Description}"));

        var chain =
            Set(() => _userInput, "input")
            | Set(tools, "tools")
            | Set(toolNames, "tool_names");

        chain = (string.IsNullOrEmpty(_characters) && !string.IsNullOrEmpty(_actionPrompt))
            ? chain | Set(_actionPrompt, "action")
            : chain | Set(_characters, "characters") | Set(_environments, "environments") |
              Set(_playerCharacter, "player_character");

        if (_gameSummary != null)
        {
            chain |= Set(_gameSummary, "summary");
        }

        if (!string.IsNullOrEmpty(_narrativeGraph))
        {
            chain |= Set(_narrativeGraph, "graph");
        }

        if (!string.IsNullOrEmpty(_graphExtensionSummary))
        {
            chain |= Set(_graphExtensionSummary, "summary");
        }

        chain = chain
                | LoadMemory(_conversationBufferMemory, "history")
                | Template(_reActPrompt)
                | LLM(_model).UseCache(_useCache)
                | UpdateMemory(_conversationBufferMemory, "input", "text")
                | ReActParser("text", ReActAnswer);

        _chain = chain;
    }

    public ReActAgentChain UseCache(bool enabled = true)
    {
        _useCache = enabled;
        return this;
    }

    public ReActAgentChain UseTool(AgentTool tool)
    {
        tool = tool ?? throw new ArgumentNullException(nameof(tool));

        _tools.Add(tool.Name, tool);
        return this;
    }

    protected override async Task<IChainValues> InternalCallAsync(IChainValues values,
        CancellationToken cancellationToken = new())
    {
        values = values ?? throw new ArgumentNullException(nameof(values));

        var input = (string)values.Value[InputKeys[0]];
        var valuesChain = new ChainValues();

        _userInput = input;

        if (_chain == null)
        {
            InitializeChain();
        }

        for (var i = 0; i < _maxActions; i++)
        {
            var res = await _chain!.CallAsync(valuesChain, cancellationToken: cancellationToken).ConfigureAwait(false);
            switch (res.Value[ReActAnswer])
            {
                case AgentAction:
                    var action = (AgentAction)res.Value[ReActAnswer];
                    var tool = _tools[action.Action.ToLower(CultureInfo.InvariantCulture)];
                    var toolRes = await tool.ToolTask(action.ActionInput, cancellationToken).ConfigureAwait(false);
                    await _conversationBufferMemory.ChatHistory
                        .AddMessage(new Message("Observation: " + toolRes, MessageRole.System))
                        .ConfigureAwait(false);
                    await _conversationBufferMemory.ChatHistory.AddMessage(new Message("Thought:", MessageRole.System))
                        .ConfigureAwait(false);
                    break;
                case AgentFinish:
                    var finish = (AgentFinish)res.Value[ReActAnswer];
                    values.Value[OutputKeys[0]] = finish.Output;
                    return values;
            }
        }

        throw new ReActChainNoFinalAnswerReachedException($"The ReAct Chain could not reach a final answer. " +
                                                          $"Values: {values}", values);
    }
}
