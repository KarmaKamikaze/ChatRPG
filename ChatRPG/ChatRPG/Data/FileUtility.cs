using System.Text;

namespace ChatRPG.Data;

public class FileUtility
{
    public FileUtility(string saveDir = "Saves/")
    {
        SaveDir = saveDir;
    }

    public string SaveDir { get; set; }

    public async Task UpdateSaveFileAsync(List<string> conversation)
    {
        string path = SetPath(DateTime.Now);
        await using FileStream fs =
            new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096,
                useAsync: true);
        foreach (string message in conversation)
        {
            byte[] encodedMessage = Encoding.Unicode.GetBytes(message);
            await fs.WriteAsync(encodedMessage, 0, encodedMessage.Length);
        }
    }

    private string SetPath(DateTime timestamp)
    {
        // Add save directory and file name to path
        string savePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, SaveDir);
        string fileName = $"conversation {timestamp:dd-mm-yyyy HH-mm-ss}.txt";
        return Path.Join(savePath, fileName);
    }
}
