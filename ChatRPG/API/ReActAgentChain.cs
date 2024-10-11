using System.Globalization;
using LangChain.Abstractions.Schema;
using LangChain.Chains.HelperChains;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Chains.StackableChains.ReAct;
using LangChain.Memory;
using LangChain.Providers;
using LangChain.Schema;
using static LangChain.Chains.Chain;

namespace ChatRPG.API;

public sealed class ReActAgentChain : BaseStackableChain
{
    private const string ReActAnswer = "answer";
    private readonly BaseChatMemory _conversationSummaryMemory;
    private readonly int _maxActions;
    private readonly IChatModel _model;
    private readonly string _reActPrompt;
    private readonly bool _useStreaming;
    private StackChain? _chain;
    private readonly Dictionary<string, AgentTool> _tools = new();
    private bool _useCache;
    private string _userInput = string.Empty;

    public string DefaultPrompt = @"Assistant is a large language model trained by OpenAI.

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

Begin!

Previous conversation history:
{history}

New input: {input}";

    public ReActAgentChain(
        IChatModel model,
        BaseChatMemory memory,
        string? reActPrompt = null,
        string inputKey = "input",
        string outputKey = "text",
        int maxActions = 10,
        bool useStreaming = true)
    {
        _model = model;
        _reActPrompt = reActPrompt ?? DefaultPrompt;
        _maxActions = maxActions;

        InputKeys = [inputKey];
        OutputKeys = [outputKey];

        _useStreaming = useStreaming;
        _conversationSummaryMemory = memory;
    }

    private void InitializeChain()
    {
        var toolNames = string.Join(",", _tools.Select(x => x.Key));
        var tools = string.Join("\n", _tools.Select(x => $"{x.Value.Name}, {x.Value.Description}"));

        var chain =
            Set(() => _userInput, "input")
            | Set(tools, "tools")
            | Set(toolNames, "tool_names")
            | LoadMemory(_conversationSummaryMemory, "history")
            | Template(_reActPrompt)
            | LLM(_model, settings: new ChatSettings
            {
                StopSequences = ["Observation", "[END]"],
                UseStreaming = _useStreaming
            }).UseCache(_useCache)
            | UpdateMemory(_conversationSummaryMemory, "input", "text")
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
                    await _conversationSummaryMemory.ChatHistory
                        .AddMessage(new Message("Observation: " + toolRes, MessageRole.System))
                        .ConfigureAwait(false);
                    await _conversationSummaryMemory.ChatHistory.AddMessage(new Message("Thought:", MessageRole.System))
                        .ConfigureAwait(false);
                    break;
                case AgentFinish:
                    var finish = (AgentFinish)res.Value[ReActAnswer];
                    values.Value[OutputKeys[0]] = finish.Output;
                    return values;
            }
        }

        return values;
    }
}
