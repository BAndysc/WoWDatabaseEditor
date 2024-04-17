using System.Collections.Generic;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class KKLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    private BoolGenericSetting adjustForGravity;
    private FloatSliderGenericSetting disconnectedMultiplier;
    private BoolGenericSetting exchangeVertices;
    private FloatSliderGenericSetting height;
    private FloatSliderGenericSetting width;
    private FloatSliderGenericSetting k;
    private FloatSliderGenericSetting lengthFactor;
    private FloatSliderGenericSetting maxIterations;
    private FloatSliderGenericSetting seed;

    public KKLayoutAlgorithmSettingsViewModel() : base("KK Layout")
    {
        adjustForGravity = new BoolGenericSetting("Adjust for gravity", false, null);
        disconnectedMultiplier = new FloatSliderGenericSetting("Disconnected multiplier", 0.5f, 0, 100, null, true);
        exchangeVertices = new BoolGenericSetting("Exchange vertices", false, null);
        height = new FloatSliderGenericSetting("Height", 30000, 0, 100000, null, true);
        width = new FloatSliderGenericSetting("Width", 30000, 0, 100000, null, true);
        k = new FloatSliderGenericSetting("K", 1, 0, 100, null, true);
        lengthFactor = new FloatSliderGenericSetting("Length factor", 1, 0, 100, null, true);
        maxIterations = new FloatSliderGenericSetting("Max iterations", 200, 0, 1000, null, true);
        seed = new FloatSliderGenericSetting("Seed", 0, 0, 1000, null, true);

        Settings.Add(adjustForGravity);
        Settings.Add(disconnectedMultiplier);
        Settings.Add(exchangeVertices);
        Settings.Add(height);
        Settings.Add(width);
        Settings.Add(k);
        Settings.Add(lengthFactor);
        Settings.Add(maxIterations);
        Settings.Add(seed);
    }

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes, IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new KKLayoutParameters()
        {
            AdjustForGravity = adjustForGravity.Value,
            DisconnectedMultiplier = disconnectedMultiplier.Value,
            ExchangeVertices = exchangeVertices.Value,
            Height = height.Value,
            K = k.Value,
            LengthFactor = lengthFactor.Value,
            MaxIterations = (int)maxIterations.Value,
            Seed = (int)seed.Value,
            Width = width.Value
        };
        return new KKLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(
            graph, vertexPositions, settings);
    }
}