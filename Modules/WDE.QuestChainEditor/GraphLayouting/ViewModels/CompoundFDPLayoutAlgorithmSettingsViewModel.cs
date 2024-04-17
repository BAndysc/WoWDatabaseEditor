using System.Collections.Generic;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using Newtonsoft.Json.Linq;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class CompoundFDPLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    private FloatSliderGenericSetting displacementLimitMultiplier;
    private FloatSliderGenericSetting elasticConstant;
    private FloatSliderGenericSetting gravitationFactor;
    private FloatSliderGenericSetting idealEdgeLength;
    private FloatSliderGenericSetting nestingFactor;
    private FloatSliderGenericSetting phase1Iterations;
    private FloatSliderGenericSetting phase2Iterations;
    private FloatSliderGenericSetting phase2TemperatureInitialMultiplier;
    private FloatSliderGenericSetting phase3Iterations;
    private FloatSliderGenericSetting phase3TemperatureInitialMultiplier;
    private FloatSliderGenericSetting repulsionConstant;
    private FloatSliderGenericSetting separationMultiplier;
    private FloatSliderGenericSetting temperatureDecreasing;
    private FloatSliderGenericSetting temperatureFactor;
    private FloatSliderGenericSetting seed;

    public CompoundFDPLayoutAlgorithmSettingsViewModel() : base("Compound FDP")
    {
        displacementLimitMultiplier = new FloatSliderGenericSetting("Displacement limit multiplier", 0.5f, 0, 5, null, false);
        elasticConstant = new FloatSliderGenericSetting("Elastic constant", 0.005f, 0, 1, null, false);
        gravitationFactor = new FloatSliderGenericSetting("Gravitation factor", 8, 0, 100, null, true);
        idealEdgeLength = new FloatSliderGenericSetting("Ideal edge length", 25, 0, 100, null, true);
        nestingFactor = new FloatSliderGenericSetting("Nesting factor", 0.2f, 0, 10, null, false);
        phase1Iterations = new FloatSliderGenericSetting("Phase 1 iterations", 50, 0, 100, null, true);
        phase2Iterations = new FloatSliderGenericSetting("Phase 2 iterations", 70, 0, 100, null, true);
        phase2TemperatureInitialMultiplier = new FloatSliderGenericSetting("Phase 2 temperature initial multiplier", 0.5f, 0, 10, null, false);
        phase3Iterations = new FloatSliderGenericSetting("Phase 3 iterations", 30, 0, 100, null, true);
        phase3TemperatureInitialMultiplier = new FloatSliderGenericSetting("Phase 3 temperature initial multiplier", 0.2f, 0, 10, null, false);
        repulsionConstant = new FloatSliderGenericSetting("Repulsion constant", 150, 0, 100, null, true);
        separationMultiplier = new FloatSliderGenericSetting("Separation multiplier", 15, 0, 100, null, true);
        temperatureDecreasing = new FloatSliderGenericSetting("Temperature decreasing", 0.5f, 0, 2, null, false);
        temperatureFactor = new FloatSliderGenericSetting("Temperature factor", 0.95f, 0, 2, null, false);
        seed = new FloatSliderGenericSetting("Seed", 0, 0, 1000, null, true);

        Settings.Add(displacementLimitMultiplier);
        Settings.Add(elasticConstant);
        Settings.Add(gravitationFactor);
        Settings.Add(idealEdgeLength);
        Settings.Add(nestingFactor);
        Settings.Add(phase1Iterations);
        Settings.Add(phase2Iterations);
        Settings.Add(phase2TemperatureInitialMultiplier);
        Settings.Add(phase3Iterations);
        Settings.Add(phase3TemperatureInitialMultiplier);
        Settings.Add(repulsionConstant);
        Settings.Add(separationMultiplier);
        Settings.Add(temperatureDecreasing);
        Settings.Add(temperatureFactor);
        Settings.Add(seed);
    }

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes, IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new CompoundFDPLayoutParameters()
        {
            DisplacementLimitMultiplier = displacementLimitMultiplier.Value,
            ElasticConstant = elasticConstant.Value,
            GravitationFactor = gravitationFactor.Value,
            IdealEdgeLength = idealEdgeLength.Value,
            NestingFactor = nestingFactor.Value,
            Phase1Iterations = (int)phase1Iterations.Value,
            Phase2Iterations = (int)phase2Iterations.Value,
            Phase2TemperatureInitialMultiplier = phase2TemperatureInitialMultiplier.Value,
            Phase3Iterations = (int)phase3Iterations.Value,
            Phase3TemperatureInitialMultiplier = phase3TemperatureInitialMultiplier.Value,
            RepulsionConstant = repulsionConstant.Value,
            SeparationMultiplier = separationMultiplier.Value,
            TemperatureDecreasing = temperatureDecreasing.Value,
            TemperatureFactor = temperatureFactor.Value,
            Seed = (int)seed.Value
        };
        return new
            CompoundFDPLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(
                graph, vertexSizes, new Dictionary<BaseQuestViewModel, Thickness>(),
                new Dictionary<BaseQuestViewModel, CompoundVertexInnerLayoutType>(), vertexPositions, settings);
    }
}