using System;
using System.Collections.Generic;
using System.Linq;
using GraphX.Common.Interfaces;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using QuickGraph.Algorithms;
using WDE.Common.Settings;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class MAGLLayoutAlgorithmSettingsViewModel : LayoutAlgorithmSettingsViewModel
{
    public override int Version => 1;

    private ListOptionGenericSetting edgeRoutingMode;
    private FloatSliderGenericSetting padding;
    private FloatSliderGenericSetting polylinePadding;
    private FloatSliderGenericSetting routingToParentConeAngle;
    private FloatSliderGenericSetting simpleSelfLoopsForParentEdgesThreshold;
    private FloatSliderGenericSetting incrementalRoutingThreshold;
    private BoolGenericSetting routeMultiEdgesAsBundles;
    private FloatSliderGenericSetting packingAspectRatio;
    private ListOptionGenericSetting packingMethod;
    private FloatSliderGenericSetting nodeSeparation;
    private FloatSliderGenericSetting clusterMargin;
    private BoolGenericSetting liftCrossEdges;
    private FloatSliderGenericSetting layerSeparation;
    private FloatSliderGenericSetting repetitionCoefficientForOrdering;
    private FloatSliderGenericSetting randomSeedForOrdering;
    private FloatSliderGenericSetting noGainAdjacentSwapStepsBound;
    private FloatSliderGenericSetting maxNumberOfPassesInOrdering;
    private FloatSliderGenericSetting groupSplit;
    private FloatSliderGenericSetting labelCornersPreserveCoefficient;
    private FloatSliderGenericSetting brandesThreshold;
    private FloatSliderGenericSetting minimalWidth;
    private FloatSliderGenericSetting minimalHeight;
    private FloatSliderGenericSetting minNodeHeight;
    private FloatSliderGenericSetting minNodeWidth;
    private FloatSliderGenericSetting aspectRatio;
    private FloatSliderGenericSetting maxAspectRatioEccentricity;
    private ListOptionGenericSetting snapToGridByY;
    private FloatSliderGenericSetting gridSizeByY;
    private FloatSliderGenericSetting gridSizeByX;

    public MAGLLayoutAlgorithmSettingsViewModel() : base("MGAL")
    {
        edgeRoutingMode = new ListOptionGenericSetting("Edge routing mode", Enum.GetValues<EdgeRoutingMode>().Cast<object>().ToList(), EdgeRoutingMode.SugiyamaSplines, null);
        padding = new FloatSliderGenericSetting("Padding", 3.0f, 0, 10, null, true);
        polylinePadding = new FloatSliderGenericSetting("Polyline padding", 1.5f, 0, 10, null, true);
        routingToParentConeAngle = new FloatSliderGenericSetting("Routing to parent cone angle", (float)Math.PI / 6.0f, 0, 2 * (float)Math.PI, null, true);
        simpleSelfLoopsForParentEdgesThreshold = new FloatSliderGenericSetting("Simple self loops for parent edges threshold", 200, 0, 1000, null, true);
        incrementalRoutingThreshold = new FloatSliderGenericSetting("Incremental routing threshold", 5000000, 0, 10000000, null, true);
        routeMultiEdgesAsBundles = new BoolGenericSetting("Route multi edges as bundles", true, null);
        packingAspectRatio = new FloatSliderGenericSetting("Packing aspect ratio", (float)(1.0 + Math.Sqrt(5.0)) / 2.0f, 0, 100, null, true);
        packingMethod = new ListOptionGenericSetting("Packing method", Enum.GetValues<PackingMethod>().Cast<object>().ToList(), PackingMethod.Compact, null);
        nodeSeparation = new FloatSliderGenericSetting("Node separation", 10.0f, 1f, 100, null, true);
        clusterMargin = new FloatSliderGenericSetting("Cluster margin", 10.0f, 0, 100, null, true);
        liftCrossEdges = new BoolGenericSetting("Lift cross edges", true, null);
        layerSeparation = new FloatSliderGenericSetting("Layer separation", 30, 0, 100, "vertical spacing", true);
        repetitionCoefficientForOrdering = new FloatSliderGenericSetting("Repetition coefficient for ordering", 1, 0, 100, null, true);
        randomSeedForOrdering = new FloatSliderGenericSetting("Random seed for ordering", 1, 0, 1000, null, true);
        noGainAdjacentSwapStepsBound = new FloatSliderGenericSetting("No gain adjacent swap steps bound", 5, 0, 100, null, true);
        maxNumberOfPassesInOrdering = new FloatSliderGenericSetting("Max number of passes in ordering", 24, 0, 100, null, true);
        groupSplit = new FloatSliderGenericSetting("Group split", 2, 0, 100, null, true);
        labelCornersPreserveCoefficient = new FloatSliderGenericSetting("Label corners preserve coefficient", 0.1f, 0, 1, null, true);
        brandesThreshold = new FloatSliderGenericSetting("Brandes threshold", 600, 0, 1000, null, true);
        minimalWidth = new FloatSliderGenericSetting("Minimal width", 0, 0, 100, null, true);
        minimalHeight = new FloatSliderGenericSetting("Minimal height", 0, 0, 100, null, true);
        minNodeHeight = new FloatSliderGenericSetting("Min node height", 9.0f, 0, 100, null, true);
        minNodeWidth = new FloatSliderGenericSetting("Min node width", 13.5f, 0, 100, null, true);
        aspectRatio = new FloatSliderGenericSetting("Aspect ratio", 0, 0, 100, null, true);
        maxAspectRatioEccentricity = new FloatSliderGenericSetting("Max aspect ratio eccentricity", 5.0f, 0, 100, null, true);
        snapToGridByY = new ListOptionGenericSetting("Snap to grid by Y", Enum.GetValues<SnapToGridByY>().Cast<object>().ToList(), SnapToGridByY.None, null);
        gridSizeByY = new FloatSliderGenericSetting("Grid size by Y", 0, 0, 100, null, true);
        gridSizeByX = new FloatSliderGenericSetting("Grid size by X", 0, 0, 100, null, true);

        Settings.Add(layerSeparation);
        Settings.Add(brandesThreshold);
        Settings.Add(minNodeHeight);
        Settings.Add(minNodeWidth);
        Settings.Add(edgeRoutingMode);
        Settings.Add(padding);
        Settings.Add(polylinePadding);
        Settings.Add(routingToParentConeAngle);
        Settings.Add(simpleSelfLoopsForParentEdgesThreshold);
        Settings.Add(incrementalRoutingThreshold);
        Settings.Add(routeMultiEdgesAsBundles);
        Settings.Add(packingAspectRatio);
        Settings.Add(packingMethod);
        Settings.Add(nodeSeparation);
        Settings.Add(clusterMargin);
        Settings.Add(liftCrossEdges);
        Settings.Add(repetitionCoefficientForOrdering);
        Settings.Add(randomSeedForOrdering);
        Settings.Add(noGainAdjacentSwapStepsBound);
        Settings.Add(maxNumberOfPassesInOrdering);
        Settings.Add(groupSplit);
        Settings.Add(labelCornersPreserveCoefficient);
        Settings.Add(minimalWidth);
        Settings.Add(minimalHeight);
        Settings.Add(aspectRatio);
        Settings.Add(maxAspectRatioEccentricity);
        Settings.Add(snapToGridByY);
        Settings.Add(gridSizeByY);
        Settings.Add(gridSizeByX);
    }

    public override ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph> Create(
        AutomaticGraphLayouter.Graph graph,
        IDictionary<BaseQuestViewModel, Size> vertexSizes,
        IDictionary<BaseQuestViewModel, Point> vertexPositions)
    {
        var settings = new SugiyamaLayoutSettings()
        {
            EdgeRoutingSettings = new EdgeRoutingSettings()
            {
                EdgeRoutingMode = (EdgeRoutingMode)edgeRoutingMode.SelectedOption,
                Padding = padding.Value,
                PolylinePadding = polylinePadding.Value,
                RoutingToParentConeAngle = routingToParentConeAngle.Value,
                SimpleSelfLoopsForParentEdgesThreshold = (int)simpleSelfLoopsForParentEdgesThreshold.Value,
                IncrementalRoutingThreshold = (int)incrementalRoutingThreshold.Value,
                RouteMultiEdgesAsBundles = routeMultiEdgesAsBundles.Value,
            },
            PackingAspectRatio = packingAspectRatio.Value,
            PackingMethod = (PackingMethod)packingMethod.SelectedOption,
            NodeSeparation = nodeSeparation.Value,
            ClusterMargin = clusterMargin.Value,
            LiftCrossEdges = liftCrossEdges.Value,
            LayerSeparation = layerSeparation.Value,
            RepetitionCoefficientForOrdering = (int)repetitionCoefficientForOrdering.Value,
            RandomSeedForOrdering = (int)randomSeedForOrdering.Value,
            NoGainAdjacentSwapStepsBound = (int)noGainAdjacentSwapStepsBound.Value,
            MaxNumberOfPassesInOrdering = (int)maxNumberOfPassesInOrdering.Value,
            GroupSplit = (int)groupSplit.Value,
            LabelCornersPreserveCoefficient = labelCornersPreserveCoefficient.Value,
            BrandesThreshold = (int)brandesThreshold.Value,
            MinimalWidth = minimalWidth.Value,
            MinimalHeight = minimalHeight.Value,
            MinNodeHeight = minNodeHeight.Value,
            MinNodeWidth = minNodeWidth.Value,
            Transformation = PlaneTransformation.UnitTransformation,
            AspectRatio = aspectRatio.Value,
            MaxAspectRatioEccentricity = maxAspectRatioEccentricity.Value,
            SnapToGridByY = (SnapToGridByY)snapToGridByY.SelectedOption,
            GridSizeByY = gridSizeByY.Value,
            GridSizeByX = gridSizeByX.Value
        };
        return new MAGLLayout<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>(graph, vertexPositions, vertexSizes, settings);
    }

    public override bool SeparateByGroups => true;
}