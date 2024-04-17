using System;
using System.Collections.Generic;
using System.Linq;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class SugiyamaLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(AutomaticGraphLayouter.Graph graph,
        IDictionary<BaseQuestViewModel, Size> vertexSizes,
        IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new SugiyamaLayoutParameters()
        {
            BaryCenteringByPosition = baryCenteringByPosition.Value,
            DirtyRound = dirtyRound.Value,
            HorizontalGap = horizontalGap.Value,
            MaxWidth = maxWidth.Value,
            Simplify = simplify.Value,
            MinimizeHierarchicalEdgeLong = minimizeHierarchicalEdgeLong.Value,
            Phase1IterationCount = (int)phase1IterationCount.Value,
            Phase2IterationCount = (int)phase2IterationCount.Value,
            PositionCalculationMethod = (PositionCalculationMethodTypes)positionCalculationMethod.SelectedOption,
            Prompting = (SugiyamaLayoutParameters.PromptingConstraintType)prompting.SelectedOption,
            Seed = (int)seed.Value
        };
        return new SugiyamaLayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(
            graph, vertexSizes, settings, _ => EdgeTypes.Hierarchical);
    }

    private BoolGenericSetting baryCenteringByPosition;
    private BoolGenericSetting dirtyRound;
    private FloatSliderGenericSetting horizontalGap;
    private FloatSliderGenericSetting maxWidth;
    private BoolGenericSetting simplify;
    private BoolGenericSetting minimizeHierarchicalEdgeLong;
    private FloatSliderGenericSetting phase1IterationCount;
    private FloatSliderGenericSetting phase2IterationCount;
    private ListOptionGenericSetting positionCalculationMethod;
    private ListOptionGenericSetting prompting;
    private FloatSliderGenericSetting seed;

    public SugiyamaLayoutAlgorithmSettingsViewModel() : base("Slow Sugiyama")
    {
        baryCenteringByPosition = new BoolGenericSetting("Barycentering by position", false, null);
        dirtyRound = new BoolGenericSetting("Dirty round", true, null);
        horizontalGap = new FloatSliderGenericSetting("Horizontal gap", 10, 0, 100, null, true);
        maxWidth = new FloatSliderGenericSetting("Max width", 10, 0, 100, null, true);
        simplify = new BoolGenericSetting("Simplify", true, null);
        minimizeHierarchicalEdgeLong = new BoolGenericSetting("Minimize hierarchical edge long", true, null);
        phase1IterationCount = new FloatSliderGenericSetting("Phase 1 iteration count", 8, 0, 100, null, true);
        phase2IterationCount = new FloatSliderGenericSetting("Phase 2 iteration count", 5, 0, 100, null, true);
        positionCalculationMethod = new ListOptionGenericSetting("Position calculation method",
            Enum.GetValues<PositionCalculationMethodTypes>().Cast<object>().ToList(), PositionCalculationMethodTypes.IndexBased, null);
        prompting = new ListOptionGenericSetting("Prompting",
            Enum.GetValues<SugiyamaLayoutParameters.PromptingConstraintType>().Cast<object>().ToList(),
            SugiyamaLayoutParameters.PromptingConstraintType.Compulsory, null);
        seed = new FloatSliderGenericSetting("Seed", 0, 0, 1000, null, true);

        Settings.Add(baryCenteringByPosition);
        Settings.Add(dirtyRound);
        Settings.Add(horizontalGap);
        Settings.Add(maxWidth);
        Settings.Add(simplify);
        Settings.Add(minimizeHierarchicalEdgeLong);
        Settings.Add(phase1IterationCount);
        Settings.Add(phase2IterationCount);
        Settings.Add(positionCalculationMethod);
        Settings.Add(prompting);
        Settings.Add(seed);
    }
}