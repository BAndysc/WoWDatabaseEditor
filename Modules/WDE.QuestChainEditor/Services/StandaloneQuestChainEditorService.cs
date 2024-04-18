using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Documents;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Services;

[SingleInstance]
[AutoRegister]
public class StandaloneQuestChainEditorService : IStandaloneQuestChainEditorService
{
    private readonly IWindowManager windowManager;
    private readonly IContainerProvider containerProvider;
    private IAbstractWindowView? openedWindow;

    public StandaloneQuestChainEditorService(IWindowManager windowManager,
        IContainerProvider containerProvider)
    {
        this.windowManager = windowManager;
        this.containerProvider = containerProvider;
    }

    public void OpenStandaloneEditor()
    {
        if (openedWindow != null)
        {
            openedWindow.Activate();
        }
        else
        {
            var viewModel = containerProvider.Resolve<QuestChainDocumentViewModel>((typeof(QuestChainSolutionItem), new QuestChainSolutionItem()));
            openedWindow = windowManager.ShowWindow(viewModel, out var task);
            WindowLifetime(viewModel, task).ListenErrors();
        }
    }

    private async Task WindowLifetime(QuestChainDocumentViewModel vm, Task task)
    {
        await task;
        vm.Dispose();
        openedWindow = null;
    }
}