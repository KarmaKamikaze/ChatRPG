using LangChain.Memory;
using LangChain.Providers;
using LangChain.Schema;
using Message = LangChain.Providers.Message;
using MessageRole = LangChain.Providers.MessageRole;

namespace ChatRPG.API.Memory;

public class ChatRPGConversationMemory(IChatModel model, string? summary) : BaseChatMemory
{
    private IChatModel Model { get; } = model ?? throw new ArgumentNullException(nameof(model));
    public string? Summary { get; private set; } = summary;
    public readonly List<(Data.Models.MessageRole, string)> Messages = new();

    public string MemoryKey { get; set; } = "history";
    public override List<string> MemoryVariables => [MemoryKey];

    public override OutputValues LoadMemoryVariables(InputValues? inputValues)
    {
        return new OutputValues(new Dictionary<string, object> { { MemoryKey, Summary ?? "" } });
    }

    public override async Task SaveContext(InputValues inputValues, OutputValues outputValues)
    {
        inputValues = inputValues ?? throw new ArgumentNullException(nameof(inputValues));
        outputValues = outputValues ?? throw new ArgumentNullException(nameof(outputValues));

        var newMessages = new List<Message>();

        // If the InputKey is not specified, there must only be one input value
        var inputKey = InputKey ?? inputValues.Value.Keys.Single();

        var humanMessageContent = inputValues.Value[inputKey].ToString() ?? string.Empty;
        newMessages.Add(new Message(humanMessageContent, MessageRole.Human));

        Messages.Add((Data.Models.MessageRole.User, humanMessageContent));

        // If the OutputKey is not specified, there must only be one output value
        var outputKey = OutputKey ?? outputValues.Value.Keys.Single();

        var aiMessageContent = outputValues.Value[outputKey].ToString() ?? string.Empty;
        int finalAnswerIndex = aiMessageContent.IndexOf("Final Answer: ", StringComparison.Ordinal);
        if (finalAnswerIndex != -1)
        {
            // Only keep final answer
            int startOutputIndex = finalAnswerIndex + "Final Answer: ".Length;
            aiMessageContent = aiMessageContent[startOutputIndex..];
        }

        newMessages.Add(new Message(aiMessageContent, MessageRole.Ai));

        Messages.Add((Data.Models.MessageRole.Assistant, aiMessageContent));

        Summary = await Model.SummarizeAsync(newMessages, Summary ?? "").ConfigureAwait(true);

        await base.SaveContext(inputValues, outputValues).ConfigureAwait(false);
    }

    public override async Task Clear()
    {
        await base.Clear().ConfigureAwait(false);
    }
}
