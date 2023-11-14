using System.Text;

namespace ChatRPG.Data;

public class FileUtility : IFileUtility
{
    private readonly string _currentUser;
    private readonly string _path;
    private readonly string _saveDir;

    // Define "special" keywords for determining the author of a message.
    private readonly string _playerKeyword = "#<Player>: ";
    private readonly string _gameKeyword = "#<Game>: ";

    /// <summary>
    /// Initializes a new instance of the FileUtility class with the specified user and save directory.
    /// </summary>
    /// <param name="currentUser">The username of the current user.</param>
    /// <param name="saveDir">The directory where conversation files are saved (default: "Saves/").</param>
    public FileUtility(string currentUser, string saveDir = "Saves/")
    {
        _currentUser = currentUser;
        _saveDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, saveDir);
        _path = SetPath(DateTime.Now);
    }

    /// <inheritdoc />
    public async Task UpdateSaveFileAsync(MessagePair messages)
    {
        // According to .NET docs, you do not need to check if directory exists first
        Directory.CreateDirectory(_saveDir);

        await using FileStream fs =
            new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096,
                useAsync: true);

        byte[] encodedPlayerMessage = Encoding.Unicode.GetBytes(PrepareMessageForSave(messages.PlayerMessage, true));
        await fs.WriteAsync(encodedPlayerMessage, 0, encodedPlayerMessage.Length);

        byte[] encodedAssistantMessage = Encoding.Unicode.GetBytes(PrepareMessageForSave(messages.AssistantMessage));
        await fs.WriteAsync(encodedAssistantMessage, 0, encodedAssistantMessage.Length);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetMostRecentConversationAsync(string playerTag, string assistantTag)
    {
        string filePath = GetMostRecentFile(Directory.GetFiles(_saveDir, $"{_currentUser}*"));
        return await GetConversationsStringFromSaveFileAsync(filePath, playerTag, assistantTag);
    }

    /// <summary>
    /// Prepares a message for saving by ensuring a newline character and specifying the author.
    /// </summary>
    /// <param name="message">The message to prepare for saving.</param>
    /// <param name="isPlayerMessage">A boolean indicating if the message is from the player (default: false).</param>
    /// <returns>The prepared message with a newline character and author tag.</returns>
    private string PrepareMessageForSave(string message, bool isPlayerMessage = false)
    {
        if (!message.EndsWith("\n"))
            message += "\n";

        message = isPlayerMessage ? $"{_playerKeyword}{message}" : $"{_gameKeyword}{message}";
        return message;
    }

    /// <summary>
    /// Reads a conversation string from a save file asynchronously and converts it into a list of messages.
    /// </summary>
    /// <param name="path">The path to the save file to read.</param>
    /// <param name="playerTag">The tag to mark a player message.</param>
    /// <param name="assistantTag">The tag to mark an assistant message.</param>
    /// <returns>A list of messages extracted from the save file.</returns>
    private async Task<List<string>> GetConversationsStringFromSaveFileAsync(string path, string playerTag,
        string assistantTag)
    {
        await using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);

        StringBuilder sb = new StringBuilder();
        byte[] buffer = new byte[0x1000];
        int bytesRead;
        while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            string stream = Encoding.Unicode.GetString(buffer, 0, bytesRead);
            sb.Append(stream);
        }

        return ConvertConversationStringToList(sb.ToString(), playerTag, assistantTag);
    }

    /// <summary>
    /// Gets the most recent file from a list of files based on their timestamps.
    /// </summary>
    /// <param name="files">An array of file paths to choose from.</param>
    /// <returns>The path to the most recent file.</returns>
    private string GetMostRecentFile(string[] files)
    {
        DateTime mostRecent = new DateTime(1, 1, 1, 0, 0, 0); // Hello Jesus
        string mostRecentPath = string.Empty;
        foreach (string filename in files)
        {
            string[] splitName = Path.GetFileNameWithoutExtension(filename).Split(' ');
            DateTime timestamp = DateTime.ParseExact(splitName[1], "dd-MM-yyyy_HH-mm-ss",
                System.Globalization.CultureInfo.InvariantCulture);
            if (DateTime.Compare(timestamp, mostRecent) > 0)
            {
                mostRecent = timestamp;
                mostRecentPath = filename;
            }
        }

        return mostRecentPath;
    }

    /// <summary>
    /// This method converts a conversation in string form to a list of messages making up the conversation.
    /// </summary>
    /// <param name="conversation">A full conversation in string form.</param>
    /// <param name="playerTag">The tag to mark a player message.</param>
    /// <param name="assistantTag">The tag to mark an assistant message.</param>
    /// <returns>The list of messages making up the conversation.</returns>
    private List<string> ConvertConversationStringToList(string conversation, string playerTag, string assistantTag)
    {
        // First split up conversation on all player keywords,
        // which gives us the player query and all subsequent assistant responses
        string[] playerSplit = conversation.Split(new[] { _playerKeyword, $"\n{_playerKeyword}" },
            StringSplitOptions.RemoveEmptyEntries);

        // Prepend "[playerTag]: " to all player queries
        for (int i = 0; i < playerSplit.Length; i++)
        {
            playerSplit[i] = $"{playerTag}: {playerSplit[i]}";
        }

        // Secondly, split all individual player strings since they still contain assistant responses
        List<string> fullConversation = new List<string>();
        foreach (string combinedMessage in playerSplit)
        {
            string[] messages = combinedMessage.Split(new[] { _gameKeyword, $"\n{_gameKeyword}" },
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < messages.Length; i++)
            {
                // Prepend "[assistantTag]: " to all assistant responses
                if (!messages[i].StartsWith($"{playerTag}: "))
                {
                    messages[i] = $"{assistantTag}: {messages[i]}";
                }

                fullConversation.Add(messages[i]);
            }
        }

        return fullConversation;
    }

    /// <summary>
    /// Sets the path for a save file based on the user and a timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp used to create the file name.</param>
    /// <returns>The complete path to the save file.</returns>
    private string SetPath(DateTime timestamp)
    {
        // Add save directory and file name to path
        string fileName = $"{_currentUser} {timestamp:dd-MM-yyyy_HH-mm-ss}.txt";
        return Path.Join(_saveDir, fileName);
    }
}
