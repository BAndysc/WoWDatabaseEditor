using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.GraphLayouting.ViewModels;
using WDE.QuestChainEditor.ViewModels;
using ConnectionViewModel = WDE.QuestChainEditor.ViewModels.ConnectionViewModel;

namespace WDE.QuestChainEditor.GraphLayouting;

[UniqueProvider]
public interface IAutomaticGraphLayouter
{
    Task DoLayout(ILayoutAlgorithmProvider algorithm, IReadOnlyList<BaseQuestViewModel> nodes, IReadOnlyList<ConnectionViewModel> connections, CancellationToken cancellationToken);
    void DoLayoutNow(ILayoutAlgorithmProvider algorithm, IReadOnlyList<BaseQuestViewModel> nodes, IReadOnlyList<ConnectionViewModel> connections, bool includeSoloNodes);
}