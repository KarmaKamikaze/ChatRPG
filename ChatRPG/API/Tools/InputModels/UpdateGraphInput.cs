namespace ChatRPG.API.Tools.InputModels;

public class UpdateGraphInput
{
    public string? SourceNodeName { get; set; }
    public string? TargetNodeName { get; set; }
    
    public bool IsValid(out List<string> validationErrors)
    {
        validationErrors = [];

        if (string.IsNullOrWhiteSpace(SourceNodeName))
            validationErrors.Add("SourceNodeName is required.");

        if (string.IsNullOrWhiteSpace(TargetNodeName))
            validationErrors.Add("TargetNodeName is required.");

        return validationErrors.Count == 0;
    }
}
