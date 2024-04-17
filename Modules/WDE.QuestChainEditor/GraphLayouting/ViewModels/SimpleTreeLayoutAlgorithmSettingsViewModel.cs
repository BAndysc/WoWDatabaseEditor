using System;
using System.Collections.Generic;
using System.Linq;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class SimpleTreeLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    private FloatSliderGenericSetting componentGap;
    private FloatSliderGenericSetting vertexGap;
    private FloatSliderGenericSetting layerGap;
    private ListOptionGenericSetting direction;
    private FloatSliderGenericSetting widthPerHeight;
    private BoolGenericSetting optimizeWidthAndHeight;
    private ListOptionGenericSetting spanningTreeGeneration;
    private FloatSliderGenericSetting seed;

    public SimpleTreeLayoutAlgorithmSettingsViewModel() : base("Simple Tree")
    {
        componentGap = new FloatSliderGenericSetting("Component gap", 10, 0, 100, null, true);
        vertexGap = new FloatSliderGenericSetting("Vertex gap", 10, 0, 100, null, true);
        layerGap = new FloatSliderGenericSetting("Layer gap", 10, 0, 100, null, true);
        direction = new ListOptionGenericSetting("Direction", Enum.GetValues<LayoutDirection>().Cast<object>().ToList(), LayoutDirection.TopToBottom, null);
        widthPerHeight = new FloatSliderGenericSetting("Width per height", 1, 0, 100, null, true);
        optimizeWidthAndHeight = new BoolGenericSetting("Optimize width and height", false, null);
        spanningTreeGeneration = new ListOptionGenericSetting("Spanning tree generation", Enum.GetValues<SpanningTreeGeneration>().Cast<object>().ToList(), SpanningTreeGeneration.BFS, null);
        seed = new FloatSliderGenericSetting("Seed", 0, 0, 1000, null, true);

        Settings.Add(componentGap);
        Settings.Add(vertexGap);
        Settings.Add(layerGap);
        Settings.Add(direction);
        Settings.Add(widthPerHeight);
        Settings.Add(optimizeWidthAndHeight);
        Settings.Add(spanningTreeGeneration);
        Settings.Add(seed);
    }

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes, IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new SimpleTreeLayoutParameters()
        {
            ComponentGap = componentGap.Value,
            Direction = (LayoutDirection)direction.SelectedOption,
            LayerGap = layerGap.Value,
            OptimizeWidthAndHeight = optimizeWidthAndHeight.Value,
            SpanningTreeGeneration = (SpanningTreeGeneration)spanningTreeGeneration.SelectedOption,
            VertexGap = vertexGap.Value,
            WidthPerHeight = widthPerHeight.Value,
            Seed = (int)seed.Value
        };
        return new SimpleTreeLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge,
            AutomaticGraphLayouter.Graph>(graph, vertexPositions, vertexSizes, settings);
    }
}