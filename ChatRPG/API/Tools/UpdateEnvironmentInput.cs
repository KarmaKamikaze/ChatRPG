namespace ChatRPG.API.Tools;

public class UpdateEnvironmentInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<UpdateCharacterInput>? Characters { get; set; }
}
