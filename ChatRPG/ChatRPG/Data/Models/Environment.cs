using System.Diagnostics.CodeAnalysis;

namespace ChatRPG.Data.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class Environment
{
    private Environment()
    {
    }

    public Environment(Campaign campaign, string name, string description)
    {
        Campaign = campaign;
        Name = name;
        Description = description;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
}
