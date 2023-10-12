namespace ChatRPG.Data.Models;

public class Environment
{
    public int Id { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}
