namespace ChatRPG.API.Tools.InputModels;

public class SearchScenarioInput
{
    public string? Query { get; set; }
    public string? NodeName { get; set; }

    public bool IsValid(out List<string> validationErrors)
    {
        validationErrors = [];

        if (string.IsNullOrWhiteSpace(Query))
        {
            validationErrors.Add("Query is required");
        }

        return validationErrors.Count == 0;
    }
}
