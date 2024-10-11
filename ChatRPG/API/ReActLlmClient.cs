using LangChain.Chains.HelperChains;
using LangChain.Chains.StackableChains.Agents;
using LangChain.Memory;
using LangChain.Prompts;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.API;

public class ReActLlmClient : IOpenAiLlmClient
{
    private readonly Gpt4Model _llm;
    private readonly ConversationBufferMemory _memory;
    private readonly PromptTemplate _promptTemplate;
    private StackChain _chain;
    private readonly ReActAgentExecutorChain _agent;

    public ReActLlmClient(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI"));
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI")!);
        _llm = new Gpt4Model(provider);
        _memory = new ConversationBufferMemory(new ChatMessageHistory());
        _chain = LoadMemory(_memory, outputKey: "history") | Template("I'm AI, hello") | LLM(_llm) |
                 UpdateMemory(_memory, requestKey: "input", responseKey: "text");
        _promptTemplate = GetTemplate();
        _agent = ReActAgentExecutor(_llm);
        //agent.UseTool();
        _llm.Settings = new OpenAiChatSettings { UseStreaming = true };

        var test = new ReActAgentChain(_llm, _memory);

    }

    public async Task<string> GetChatCompletion(IList<OpenAiGptMessage> inputs, string systemPrompt)
    {
        var chain = Set() | _agent;
        return (await chain.RunAsync("text"))!;
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<string> GetStreamedChatCompletion(IList<OpenAiGptMessage> inputs, string systemPrompt)
    {
        var eventProcessor = new LlmEventProcessor(_llm);

        var chain = Set() | _agent;

        _ = Task.Run(async () => await chain.RunAsync());

        return eventProcessor.GetContentStreamAsync();
    }

    private PromptTemplate GetTemplate()
    {
        return new PromptTemplate(new PromptTemplateInput(
            template: @"Assistant is a large language model trained by OpenAI.

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

Begin!

Previous conversation history:
{chat_history}

New input: {input}
{agent_scratchpad}",
            inputVariables: new[] { "tools", "tool_names", "chat_history", "input", "agent_scratchpad" }));
    }
}
