using System.Collections.Generic;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class ISOMLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    private FloatSliderGenericSetting coolingFactor;
    private FloatSliderGenericSetting height;
    private FloatSliderGenericSetting initialAdaption;
    private FloatSliderGenericSetting initialRadius;
    private FloatSliderGenericSetting maxEpoch;
    private FloatSliderGenericSetting minRadius;
    private FloatSliderGenericSetting minAdaption;
    private FloatSliderGenericSetting radiusConstantTime;
    private FloatSliderGenericSetting seed;
    private FloatSliderGenericSetting width;

    public ISOMLayoutAlgorithmSettingsViewModel() : base("ISOM")
    {
        coolingFactor = new FloatSliderGenericSetting("Cooling factor", 2, 0, 20, null, false);
        height = new FloatSliderGenericSetting("Height", 3000, 0, 300000, null, true);
        initialAdaption = new FloatSliderGenericSetting("Initial adaption", 0.9f, 0, 2, null, false);
        initialRadius = new FloatSliderGenericSetting("Initial radius", 5, 0, 100, null, true);
        maxEpoch = new FloatSliderGenericSetting("Max epoch", 2000, 0, 10000, null, true);
        minRadius = new FloatSliderGenericSetting("Min radius", 0, 0, 100, null, true);
        minAdaption = new FloatSliderGenericSetting("Min adaption", 0, 0, 1, null, true);
        radiusConstantTime = new FloatSliderGenericSetting("Radius constant time", 100, 0, 1000, null, true);
        seed = new FloatSliderGenericSetting("Seed", 0, 0, 1000, null, true);
        width = new FloatSliderGenericSetting("Width", 3000, 0, 300000, null, true);

        Settings.Add(coolingFactor);
        Settings.Add(height);
        Settings.Add(initialAdaption);
        Settings.Add(initialRadius);
        Settings.Add(maxEpoch);
        Settings.Add(minRadius);
        Settings.Add(minAdaption);
        Settings.Add(radiusConstantTime);
        Settings.Add(seed);
        Settings.Add(width);
    }

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes, IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new ISOMLayoutParameters()
        {
            CoolingFactor = coolingFactor.Value,
            Height = height.Value,
            InitialAdaption = initialAdaption.Value,
            InitialRadius = (int)initialRadius.Value,
            MaxEpoch = (int)maxEpoch.Value,
            MinRadius = (int)minRadius.Value,
            MinAdaption = minAdaption.Value,
            RadiusConstantTime = (int)radiusConstantTime.Value,
            Seed = (int)seed.Value,
            Width = width.Value
        };
        return new ISOMLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(
            graph, vertexPositions, settings);
    }
}