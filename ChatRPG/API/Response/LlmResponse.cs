namespace ChatRPG.API.Response;

public class LlmResponse
{
    public string? Narrative { get; set; }
    public List<LlmResponseCharacter>? Characters { get; set; }
    public List<LlmResponseEvent>? Events { get; set; }
    public LlmResponseEnvironment? Environment { get; set; }
    public bool? IsInCombat { get; set; }
}
