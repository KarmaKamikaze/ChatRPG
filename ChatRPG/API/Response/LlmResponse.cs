namespace ChatRPG.API.Response;

public class LlmResponse
{
    public string? Narrative { get; set; }
    public List<LlmResponseCharacter>? Characters { get; set; }
    public LlmResponseEnvironment? Environment { get; set; }
    public bool? IsInCombat { get; set; }
    public string? Opponent { get; set; }
    public bool? Hurt { get; set; }
    public bool? Heal { get; set; }
}
