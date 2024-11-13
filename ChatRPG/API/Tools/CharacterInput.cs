namespace ChatRPG.API.Tools;

public class CharacterInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? State { get; set; }

    public bool IsValidForBattle(out List<string> validationErrors)
    {
        validationErrors = [];

        if (string.IsNullOrWhiteSpace(Name))
            validationErrors.Add("Name is required.");

        if (string.IsNullOrWhiteSpace(Description))
            validationErrors.Add("Description is required.");

        return validationErrors.Count == 0;
    }
}
