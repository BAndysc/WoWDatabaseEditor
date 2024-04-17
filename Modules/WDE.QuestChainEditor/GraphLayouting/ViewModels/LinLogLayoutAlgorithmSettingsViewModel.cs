using System.Collections.Generic;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class LinLogLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    private FloatSliderGenericSetting attractionExponent;
    private FloatSliderGenericSetting gravitationMultiplier;
    private FloatSliderGenericSetting repulsiveExponent;
    private FloatSliderGenericSetting seed;

    public LinLogLayoutAlgorithmSettingsViewModel() : base("Lin log")
    {
        attractionExponent = new FloatSliderGenericSetting("Attraction exponent", 1, 0, 100, null, true);
        gravitationMultiplier = new FloatSliderGenericSetting("Gravitation multiplier", 0.1f, 0, 10, null);
        repulsiveExponent = new FloatSliderGenericSetting("Repulsive exponent", 0, 0, 100, null, true);
        seed = new FloatSliderGenericSetting("Seed", 0, 0, 1000, null, true);

        Settings.Add(attractionExponent);
        Settings.Add(gravitationMultiplier);
        Settings.Add(repulsiveExponent);
        Settings.Add(seed);
    }

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes, IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new LinLogLayoutParameters()
        {
            AttractionExponent = attractionExponent.Value,
            GravitationMultiplier = gravitationMultiplier.Value,
            RepulsiveExponent = repulsiveExponent.Value,
            Seed = (int)seed.Value
        };
        return new LinLogLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(
            graph, vertexPositions, settings);
    }
}