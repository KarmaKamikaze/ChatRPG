namespace ChatRPG.Data;

public interface IFileUtility
{
    /// <summary>
    /// Updates the conversation save file asynchronously with the provided message pair.
    /// </summary>
    /// <param name="messages">A pair of messages (player and assistant) to save in the file.</param>
    /// <returns>A task representing the asynchronous file update process.</returns>
    Task UpdateSaveFileAsync(MessagePair messages);
    /// <summary>
    /// Retrieves the most recent conversation from a save file asynchronously, parsing and inserting
    /// into messages the player and assistant tags.
    /// </summary>
    /// <param name="playerTag">The tag to mark a player message.</param>
    /// <param name="assistantTag">The tag to mark an assistant message.</param>
    /// <returns>A list of messages representing the most recent conversation.</returns>
    Task<List<string>> GetMostRecentConversationAsync(string playerTag, string assistantTag);
}

public record MessagePair(string PlayerMessage, string AssistantMessage);
