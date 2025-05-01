namespace ChatRPG.API.Tools.InputModels;

public class AddNodeInput
{
    public string? Name { get; set; }
    public string? StoryContent { get; set; }
    public List<AddEdgeInput>? Edges { get; set; }

    public bool IsValid(out List<string> validationErrors)
    {
        validationErrors = [];

        if (string.IsNullOrWhiteSpace(Name))
            validationErrors.Add("Name is required.");

        if (string.IsNullOrWhiteSpace(StoryContent))
            validationErrors.Add("StoryContent is required.");

        if (Edges is null || Edges.Count == 0)
        {
            validationErrors.Add("Edges list is required.");
        }
        else
        {
            foreach (var edge in Edges)
            {
                if (!edge.IsValid(out var edgeErrors))
                {
                    validationErrors.AddRange(edgeErrors.Select(e => $"Edge: {e}"));
                }
            }
        }

        return validationErrors.Count == 0;
    }
}
