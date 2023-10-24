using System.Text;

namespace ChatRPG.Data;

public class FileUtility
{
    private readonly string _path;
    private readonly string _saveDir;
    private readonly string _filenamePrefix = "conversation";
    // Define "special" keywords for determining the author of a message.
    private readonly string _playerKeyword = "#<Player>: ";
    private readonly string _gameKeyword = "#<Game>: ";

    public FileUtility(string saveDir = "Saves/")
    {
        _saveDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, saveDir);
        _path = SetPath(DateTime.Now);
    }

    public async Task UpdateSaveFileAsync(string message, bool isPLayerMessage = false)
    {
        // According to .NET docs, you do not need to check if directory exists first
        Directory.CreateDirectory(_saveDir);

        await using FileStream fs =
            new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096,
                useAsync: true);

        if (!message.EndsWith("\n"))
            message += "\n";

        message = isPLayerMessage ? $"{_playerKeyword}{message}" : $"{_gameKeyword}{message}";

        byte[] encodedMessage = Encoding.Unicode.GetBytes(message);
        await fs.WriteAsync(encodedMessage, 0, encodedMessage.Length);
    }

    public async Task<List<string>> GetMostRecentConversationAsync()
    {
        string filePath = GetMostRecentFile(Directory.GetFiles(_saveDir, $"{_filenamePrefix}*"));
        return await GetConversationsStringFromSaveFileAsync(filePath);
    }

    private async Task<List<string>> GetConversationsStringFromSaveFileAsync(string path)
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

        return ConvertConversationStringToList(sb.ToString());
    }

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
    /// Importantly, this method assumes that a conversation is initiated by the player and that the conversation
    /// alternates between the player and the game.
    /// </summary>
    /// <param name="conversation">A full conversation in string form.</param>
    /// <returns>The list of messages making up the conversation.</returns>
    private List<string> ConvertConversationStringToList(string conversation)
    {
        string[] conversationSplit =
            conversation.Split(
                new[] { _playerKeyword, _gameKeyword, $"\n{_playerKeyword}", $"\n{_gameKeyword}" },
                StringSplitOptions.RemoveEmptyEntries);
        List<string> conversationList = new List<string>();

        for (int i = 0; i < conversationSplit.Length; i++)
        {
            conversationList.Add(i % 2 == 0 ? $"You: {conversationSplit[i]}" : $"Game: {conversationSplit[i]}");
        }

        return conversationList;
    }

    private string SetPath(DateTime timestamp)
    {
        // Add save directory and file name to path
        string fileName = $"{_filenamePrefix} {timestamp:dd-MM-yyyy_HH-mm-ss}.txt";
        return Path.Join(_saveDir, fileName);
    }
}
