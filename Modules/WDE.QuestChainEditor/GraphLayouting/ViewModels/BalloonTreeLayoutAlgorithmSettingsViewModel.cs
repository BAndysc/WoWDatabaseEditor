using System.Collections.Generic;
using System.Linq;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using Newtonsoft.Json.Linq;
using QuickGraph.Algorithms;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class BalloonTreeLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    public BalloonTreeLayoutAlgorithmSettingsViewModel() : base("Balloon tree")
    {
        minRadius = new FloatSliderGenericSetting("Min radius", 2, 0, 100, null, true);
        border = new FloatSliderGenericSetting("Border", 20, 0, 100, null, true);

        Settings.Add(minRadius);
        Settings.Add(border);
    }

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes, IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new BalloonTreeLayoutParameters()
        {
            MinRadius = (int)minRadius.Value,
            Border = border.Value
        };
        var root = graph.Roots().FirstOrDefault();

        if (root == null)
            return new NullLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(graph);

        return new BalloonTreeLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(graph, vertexPositions, settings, root);
    }

    private FloatSliderGenericSetting minRadius;
    private FloatSliderGenericSetting border;
}