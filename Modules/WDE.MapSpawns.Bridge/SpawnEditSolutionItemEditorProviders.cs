using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.MapSpawns.Bridge.ViewModels;
using WDE.MapSpawns.Models.Solution;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Bridge;

// Opening one of the 3D-edit solution items (from the session panel / solution explorer) shows a
// read-only preview document — the actual editing happens in the 3D map view.

[AutoRegister]
public class FormationsSolutionItemEditorProvider : ISolutionItemEditorProvider<FormationsSolutionItem>
{
    private readonly IContainerProvider containerProvider;

    public FormationsSolutionItemEditorProvider(IContainerProvider containerProvider)
    {
        this.containerProvider = containerProvider;
    }

    public IDocument GetEditor(FormationsSolutionItem item) =>
        containerProvider.Resolve<FormationsDocumentViewModel>((typeof(FormationsSolutionItem), item));
}

[AutoRegister]
public class SpawnGroupsSolutionItemEditorProvider : ISolutionItemEditorProvider<SpawnGroupsSolutionItem>
{
    private readonly IContainerProvider containerProvider;

    public SpawnGroupsSolutionItemEditorProvider(IContainerProvider containerProvider)
    {
        this.containerProvider = containerProvider;
    }

    public IDocument GetEditor(SpawnGroupsSolutionItem item) =>
        containerProvider.Resolve<SpawnGroupsDocumentViewModel>((typeof(SpawnGroupsSolutionItem), item));
}

[AutoRegister]
public class PoolsSolutionItemEditorProvider : ISolutionItemEditorProvider<PoolsSolutionItem>
{
    private readonly IContainerProvider containerProvider;

    public PoolsSolutionItemEditorProvider(IContainerProvider containerProvider)
    {
        this.containerProvider = containerProvider;
    }

    public IDocument GetEditor(PoolsSolutionItem item) =>
        containerProvider.Resolve<PoolsDocumentViewModel>((typeof(PoolsSolutionItem), item));
}

[AutoRegister]
public class WaypointsSolutionItemEditorProvider : ISolutionItemEditorProvider<WaypointsSolutionItem>
{
    private readonly IContainerProvider containerProvider;

    public WaypointsSolutionItemEditorProvider(IContainerProvider containerProvider)
    {
        this.containerProvider = containerProvider;
    }

    public IDocument GetEditor(WaypointsSolutionItem item) =>
        containerProvider.Resolve<WaypointsDocumentViewModel>((typeof(WaypointsSolutionItem), item));
}
