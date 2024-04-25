using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Documents;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Services;

[SingleInstance]
[AutoRegisterToParentScope]
public class StandaloneQuestChainEditorService : IStandaloneQuestChainEditorService
{
    private readonly IWindowManager windowManager;
    private readonly IContainerProvider containerProvider;
    private List<IAbstractWindowView> openedWindows = new();

    public List<QuestChainDocumentViewModel> OpenedDocuments { get; } = new();

    public StandaloneQuestChainEditorService(IWindowManager windowManager,
        IContainerProvider containerProvider)
    {
        this.windowManager = windowManager;
        this.containerProvider = containerProvider;
    }

    private IAbstractWindowView? FindExistingWindow(uint? quest)
    {
        if (quest == null)
            return null;

        for (int i = 0; i < openedWindows.Count; ++i)
        {
            if (OpenedDocuments[i].HasQuest(quest.Value))
            {
                return openedWindows[i];
            }
        }

        return null;
    }

    public void OpenStandaloneEditor(uint? entry = null)
    {
        if (FindExistingWindow(entry) is { } existingWindow)
        {
            existingWindow.Activate();
        }
        else if (!entry.HasValue && openedWindows.Count > 0)
        {
            var openedWindow = openedWindows[0];
            openedWindow.Activate();
        }
        else
        {
            var solutionItem = new QuestChainSolutionItem();
            if (entry.HasValue)
                solutionItem.AddEntry(entry.Value);
            var viewModel = containerProvider.Resolve<StandaloneQuestChainEditorViewModel>((typeof(QuestChainSolutionItem), solutionItem));
            OpenedDocuments.Add(viewModel.ViewModel);
            var window = windowManager.ShowWindow(viewModel, out var task);
            openedWindows.Add(window);
            WindowLifetime(viewModel, window, task).ListenErrors();
        }
    }

    private async Task WindowLifetime(StandaloneQuestChainEditorViewModel vm, IAbstractWindowView window, Task task)
    {
        await task;
        OpenedDocuments.Remove(vm.ViewModel);
        openedWindows.Remove(window);
        vm.Dispose();
    }
}