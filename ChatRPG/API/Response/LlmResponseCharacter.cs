namespace ChatRPG.API.Response;

public class LlmResponseCharacter
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int HealthPoints { get; set; }
}
