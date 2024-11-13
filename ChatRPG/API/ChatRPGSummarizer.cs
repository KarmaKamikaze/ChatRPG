using LangChain.Memory;
using LangChain.Providers;
using static LangChain.Chains.Chain;

namespace ChatRPG.API;

// ReSharper disable once InconsistentNaming
public static class ChatRPGSummarizer
{
    public const string SummaryPrompt = @"
Progressively summarize the interaction between the player and the GM. Append to the summary so that new messages are most represented, while still remembering key details of the far past. The player describes their actions in response to the game world, and the GM narrates the outcome, revealing the next part of the adventure. Return a new summary based on each exchange taking into account the previous summary. Expand the summary so that it is long enough to capture the essence of the story from the beginning without forgetting key details. Do not remove important people, events, or locations from the summary.

EXAMPLE
Current summary:
The player enters the forest and cautiously looks around. The GM describes towering trees and a narrow path leading deeper into the woods. The player decides to follow the path, staying alert.

New lines of conversation:
Player: I move carefully down the path, keeping an eye out for any hidden dangers.
GM: As you continue, the air grows colder, and you hear rustling in the bushes ahead. Suddenly, a shadowy figure leaps out in front of you.

New summary:
The player enters the forest and follows a narrow path, staying alert. The GM introduces a shadowy figure that appears ahead after rustling is heard in the bushes.
END OF EXAMPLE

Current summary:
{summary}

New lines of conversation:
{new_lines}

New summary:";

    public static async Task<string> SummarizeAsync(
        this IChatModel chatModel,
        IEnumerable<Message> newMessages,
        string existingSummary,
        MessageFormatter? formatter = null,
        CancellationToken cancellationToken = default)
    {
        formatter ??= new MessageFormatter();
        formatter.HumanPrefix = "Player";
        formatter.AiPrefix = "GM";

        var newLines = formatter.Format(newMessages);

        var chain =
            Set(existingSummary, outputKey: "summary")
            | Set(newLines, outputKey: "new_lines")
            | Template(SummaryPrompt)
            | LLM(chatModel);

        return await chain.RunAsync("text", cancellationToken: cancellationToken).ConfigureAwait(true) ?? string.Empty;
    }
}
