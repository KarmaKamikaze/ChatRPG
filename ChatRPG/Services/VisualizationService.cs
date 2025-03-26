using ChatRPG.Data.Models;
using ChatRPG.Visualization;

namespace ChatRPG.Services;

public class VisualizationService(IConfiguration configuration)
{
    private bool _shouldVisualizeNarrativeGraph = configuration.GetValue<bool>("ShouldVisualizeNarrativeGraph");

    public void VisualizeNarrativeGraphIfEnabled(NarrativeGraph graph)
    {
        if (_shouldVisualizeNarrativeGraph)
        {
            GraphVisualization.SaveToPngFile(graph);
        }
    }
}
