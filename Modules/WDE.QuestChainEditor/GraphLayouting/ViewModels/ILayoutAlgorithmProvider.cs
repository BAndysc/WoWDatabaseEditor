using System.Collections.Generic;
using GraphX.Common.Interfaces;
using GraphX.Measure;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public interface ILayoutAlgorithmProvider
{
    ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>
        Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes,
            IDictionary<BaseQuestViewModel, Point> vertexPositions);

    bool SeparateByGroups { get; }
}