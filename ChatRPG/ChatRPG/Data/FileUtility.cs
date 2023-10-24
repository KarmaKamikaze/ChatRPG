using System.Text;

namespace ChatRPG.Data;

public class FileUtility
{
    private readonly string _path;
    private readonly string _saveDir;
    private readonly string _filenamePrefix = "conversation";

    public FileUtility(string saveDir = "Saves/")
    {
        _saveDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, saveDir);
        _path = SetPath(DateTime.Now);
    }

    public async Task UpdateSaveFileAsync(string message)
    {
        // According to .NET docs, you do not need to check if directory exists first
        Directory.CreateDirectory(_saveDir);

        await using FileStream fs =
            new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096,
                useAsync: true);

        if (!message.EndsWith("\n"))
            message += "\n";

        byte[] encodedMessage = Encoding.Unicode.GetBytes(message);
        await fs.WriteAsync(encodedMessage, 0, encodedMessage.Length);
    }

    public async Task<string> GetConversationFromSaveFileAsync(string path)
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

        return sb.ToString();
    }

    public async Task<string> GetMostRecentConversationAsync()
    {
        string filePath = GetMostRecentFile(Directory.GetFiles(_saveDir, $"{_filenamePrefix}*"));
        return await GetConversationFromSaveFileAsync(filePath);
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

    private string SetPath(DateTime timestamp)
    {
        // Add save directory and file name to path
        string fileName = $"{_filenamePrefix} {timestamp:dd-MM-yyyy_HH-mm-ss}.txt";
        return Path.Join(_saveDir, fileName);
    }
}
