using ChatRPG.Data.Models;
using ChatRPG.Services;
using LangChain.Memory;
using LangChain.Providers;
using LangChain.Schema;
using Microsoft.AspNetCore.Components;
using Message = LangChain.Providers.Message;
using MessageRole = LangChain.Providers.MessageRole;

namespace ChatRPG.API.Memory;

public class ChatRPGConversationMemory : BaseChatMemory
{
    private string SummaryText { get; set; } = string.Empty;
    private IChatModel Model { get; }
    private Campaign Campaign { get; }
    [Inject] private GameStateManager? GameStateManager { get; set; }

    public string MemoryKey { get; set; } = "history";
    public override List<string> MemoryVariables => new List<string> { MemoryKey };

    public ChatRPGConversationMemory(Campaign campaign, IChatModel model)
    {
        Campaign = campaign;
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public ChatRPGConversationMemory(Campaign campaign, IChatModel model, string savedSummaryText)
    {
        Campaign = campaign;
        Model = model ?? throw new ArgumentNullException(nameof(model));
        SummaryText = savedSummaryText ?? throw new ArgumentNullException(nameof(savedSummaryText));
    }


    public override OutputValues LoadMemoryVariables(InputValues? inputValues)
    {
        return new OutputValues(new Dictionary<string, object> { { MemoryKey, SummaryText } });
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
        Campaign.Messages.Add(new Data.Models.Message(Campaign, Data.Models.MessageRole.User, humanMessageContent));

        // If the OutputKey is not specified, there must only be one output value
        var outputKey = OutputKey ?? outputValues.Value.Keys.Single();

        var aiMessageContent = outputValues.Value[outputKey].ToString() ?? string.Empty;
        newMessages.Add(new Message(aiMessageContent, MessageRole.Ai));
        Campaign.Messages.Add(new Data.Models.Message(Campaign, Data.Models.MessageRole.Assistant, aiMessageContent));

        SummaryText = await Model.SummarizeAsync(newMessages, SummaryText).ConfigureAwait(false);
        Campaign.GameSummary = SummaryText;

        await GameStateManager!.SaveCurrentState(Campaign);
    }

    public override async Task Clear()
    {
        await base.Clear().ConfigureAwait(false);
        SummaryText = string.Empty;
    }
}
