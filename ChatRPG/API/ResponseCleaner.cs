namespace ChatRPG.API;

public class ResponseCleaner
{
    public static string RemoveMarkdown(string text)
    {
        if (text.StartsWith("```json") && text.EndsWith("```"))
        {
            text = text.Replace("```json", "");
            text = text.Replace("```", "");
        }
        return text;
    }
}