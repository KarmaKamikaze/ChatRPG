namespace ChatRPG.API.Tools;

public class AddEndNodeInput
{
    public string? SourceNodeName { get; set; }
    public List<string>? Conditions { get; set; }

    public bool IsValid(out List<string> validationErrors)
    {
        validationErrors = [];

        if (string.IsNullOrWhiteSpace(SourceNodeName))
        {
            validationErrors.Add("SourceNodeName is required");
        }

        if (Conditions is null || Conditions.Count == 0)
        {
            validationErrors.Add("Conditions list is required.");
        }
        else
        {
            foreach (var condition in Conditions)
            {
                if (string.IsNullOrWhiteSpace(condition))
                {
                    validationErrors.Add("Condition is required.");
                }
            }
        }

        return validationErrors.Count == 0;
    }
}
