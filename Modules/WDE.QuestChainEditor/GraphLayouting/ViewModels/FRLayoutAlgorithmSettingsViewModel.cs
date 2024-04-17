using System;
using System.Collections.Generic;
using System.Linq;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class FRLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    private FloatSliderGenericSetting attractionMultiplier;
    private ListOptionGenericSetting coolingFunction;
    private FloatSliderGenericSetting idealEdgeLength;
    private FloatSliderGenericSetting iterationLimit;
    private FloatSliderGenericSetting lambda;
    private FloatSliderGenericSetting repulsiveMultiplier;
    private FloatSliderGenericSetting seed;

    public FRLayoutAlgorithmSettingsViewModel() : base("FR Layout")
    {
        attractionMultiplier = new FloatSliderGenericSetting("Attraction multiplier", 1.2f, 0, 100, null, true);
        coolingFunction = new ListOptionGenericSetting("Cooling function", Enum.GetValues<FRCoolingFunction>().Cast<object>().ToList(), FRCoolingFunction.Exponential, null);
        idealEdgeLength = new FloatSliderGenericSetting("Ideal edge length", 10, 0, 100, null, true);
        iterationLimit = new FloatSliderGenericSetting("Iteration limit", 200, 0, 1000, null, true);
        lambda = new FloatSliderGenericSetting("Lambda", 0.95f, 0, 1, null, true);
        repulsiveMultiplier = new FloatSliderGenericSetting("Repulsive multiplier", 0.6f, 0, 100, null, true);
        seed = new FloatSliderGenericSetting("Seed", 0, 0, 1000, null, true);

        Settings.Add(attractionMultiplier);
        Settings.Add(coolingFunction);
        Settings.Add(idealEdgeLength);
        Settings.Add(iterationLimit);
        Settings.Add(lambda);
        Settings.Add(repulsiveMultiplier);
        Settings.Add(seed);
    }

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes, IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new FreeFRLayoutParameters()
        {
            AttractionMultiplier = attractionMultiplier.Value,
            CoolingFunction = (FRCoolingFunction)coolingFunction.SelectedOption,
            IdealEdgeLength = idealEdgeLength.Value,
            IterationLimit = (int)iterationLimit.Value,
            Lambda = lambda.Value,
            RepulsiveMultiplier = repulsiveMultiplier.Value,
            Seed = (int)seed.Value
        };
        return new FRLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(
            graph, vertexPositions, settings);
    }
}