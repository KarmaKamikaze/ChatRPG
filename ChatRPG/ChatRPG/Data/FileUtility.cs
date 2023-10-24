using System.Text;

namespace ChatRPG.Data;

public class FileUtility
{
    private readonly string _path;
    private readonly string _saveDir;
    public FileUtility(string saveDir = "Saves/")
    {
        _saveDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, saveDir);
        _path = SetPath(DateTime.Now);
    }

    public async Task UpdateSaveFileAsync(string message)
    {
        // According to .NET docs, you do not need to deck if it exists first
        Directory.CreateDirectory(_saveDir);

        await using FileStream fs =
            new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096,
                useAsync: true);

        if (!message.EndsWith("\n"))
            message += "\n";

        byte[] encodedMessage = Encoding.Unicode.GetBytes(message);
        await fs.WriteAsync(encodedMessage, 0, encodedMessage.Length);
    }

    private string SetPath(DateTime timestamp)
    {
        // Add save directory and file name to path
        string fileName = $"conversation {timestamp:dd-mm-yyyy HH-mm-ss}.txt";
        return Path.Join(_saveDir, fileName);
    }
}
