using System;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.LootEditor.Editor.Standalone;
using WDE.LootEditor.Solution;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Services;

[AutoRegister]
public class LootService : ILootService
{
    private readonly IWindowManager windowManager;
    private readonly IEventAggregator eventAggregator;
    private readonly Func<StandaloneLootEditorViewModel> creator;

    private IAbstractWindowView? currentWindow;
    
    public LootService(IWindowManager windowManager,
        IEventAggregator eventAggregator,
        Func<StandaloneLootEditorViewModel> creator)
    {
        this.windowManager = windowManager;
        this.eventAggregator = eventAggregator;
        this.creator = creator;
    }
    
    public async Task OpenStandaloneLootEditor()
    {
        if (currentWindow != null)
        {
            currentWindow.Activate();
        }
        else
        {
            using var vm = creator();
            vm.LootType = LootSourceType.Creature;
            vm.SolutionEntry = 1;
            currentWindow = windowManager.ShowWindow(vm, out var task);
            vm.LoadLootCommand.Execute(null);
            await WindowLifetimeTask(currentWindow, task);
        }
    }

    private async Task WindowLifetimeTask(IAbstractWindowView window, Task lifetime)
    {
        await lifetime;
        currentWindow = null;
    }

    public async Task EditLoot(LootSourceType type, uint entry, uint difficultyId)
    {
        using var standalone = creator();
        standalone.SetWithoutLoading(type, entry, standalone.AllDifficulties.FirstOrDefault(d => d.Id == difficultyId) ?? standalone.Difficulty);
        var task = windowManager.ShowDialog(standalone);
        await standalone.LoadLootCommand.ExecuteAsync();
        await task;
    }

    // public void OpenLootDocument(LootSourceType type, uint solutionEntry)
    // {
    //     var solutionItem = new LootSolutionItem(type, solutionEntry);
    //     eventAggregator.GetEvent<EventRequestOpenItem>().Publish(solutionItem);
    // }
}