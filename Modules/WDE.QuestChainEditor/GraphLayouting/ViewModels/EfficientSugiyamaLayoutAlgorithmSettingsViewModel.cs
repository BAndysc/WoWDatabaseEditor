using System;
using System.Collections.Generic;
using System.Linq;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using Newtonsoft.Json.Linq;
using WDE.Common.Settings;
using WDE.Common.Utils;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class EfficientSugiyamaLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;
    
    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(
        AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes,
        IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new EfficientSugiyamaLayoutParameters()
        {
            Direction = (LayoutDirection)direction.SelectedOption,
            LayerDistance = layerDistance.Value, // vertical gap
            VertexDistance = vertexDistance.Value, // horizontal gap
            PositionMode = (int)positionMode.SelectedOption,
            OptimizeWidth = optimizeWidth.Value, // no difference
            WidthPerHeight = widthPerHeight.Value, // no difference
            MinimizeEdgeLength = minimizeEdgeLength.Value, // no difference
            EdgeRouting = (SugiyamaEdgeRoutings)edgeRouting.SelectedOption, // no difference
            Seed = (int)seed.Value
        };
        return new EfficientSugiyamaLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge,
            AutomaticGraphLayouter.Graph>(
            graph, settings, vertexPositions, vertexSizes);
    }

    private ListOptionGenericSetting direction;
    private FloatSliderGenericSetting layerDistance;
    private FloatSliderGenericSetting vertexDistance;
    private ListOptionGenericSetting positionMode;
    private BoolGenericSetting optimizeWidth;
    private FloatSliderGenericSetting widthPerHeight;
    private BoolGenericSetting minimizeEdgeLength;
    private ListOptionGenericSetting edgeRouting;
    private FloatSliderGenericSetting seed;

    public EfficientSugiyamaLayoutAlgorithmSettingsViewModel() : base("Efficient Sugiyama")
    {
        direction = new ListOptionGenericSetting("Direction", Enum.GetValues<LayoutDirection>().Cast<object>().ToList(),
            LayoutDirection.TopToBottom, null);
        layerDistance = new FloatSliderGenericSetting("Layer distance", 15, 0, 100, "Vertical gap", true);
        vertexDistance = new FloatSliderGenericSetting("Vertex distance", 35, 0, 100, "Horizontal gap", true);
        positionMode = new ListOptionGenericSetting("Position mode", new List<object> { -1, 0, 1, 2, 3 }, 3, null);
        optimizeWidth = new BoolGenericSetting("Optimize width", false, null);
        widthPerHeight = new FloatSliderGenericSetting("Width per height", 100, 0, 1000, null, true);
        minimizeEdgeLength = new BoolGenericSetting("Minimize edge length", true, null);
        edgeRouting = new ListOptionGenericSetting("Edge routing",
            Enum.GetValues<SugiyamaEdgeRoutings>().Cast<object>().ToList(), SugiyamaEdgeRoutings.Orthogonal, null);
        seed = new FloatSliderGenericSetting("Seed", 0, 0, 1000, null, true);

        Settings.Add(direction);
        Settings.Add(layerDistance);
        Settings.Add(vertexDistance);
        Settings.Add(positionMode);
        Settings.Add(optimizeWidth);
        Settings.Add(widthPerHeight);
        Settings.Add(minimizeEdgeLength);
        Settings.Add(edgeRouting);
        Settings.Add(seed);
    }
}