using System.Diagnostics.CodeAnalysis;

namespace ChatRPG.Data.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class StartScenario
{
    private StartScenario()
    {
    }

    public StartScenario(string title, string body)
    {
        Title = title;
        Body = body;
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
}
