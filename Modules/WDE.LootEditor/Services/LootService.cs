using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.Standalone;
using WDE.LootEditor.Solution;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Services;

[AutoRegister]
public class LootService : ILootService
{
    private readonly IWindowManager windowManager;
    private readonly IEventAggregator eventAggregator;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly Func<StandaloneLootEditorViewModel> creator;

    private IAbstractWindowView? currentWindow;
    private Dictionary<(LootSourceType, uint), IAbstractWindowView> typedWindows = new();
    private bool onlyOneWindow = false;
    
    public LootService(IWindowManager windowManager,
        IEventAggregator eventAggregator,
        ICurrentCoreVersion currentCoreVersion,
        Func<StandaloneLootEditorViewModel> creator)
    {
        this.windowManager = windowManager;
        this.eventAggregator = eventAggregator;
        this.currentCoreVersion = currentCoreVersion;
        this.creator = creator;
    }
    
    public async Task OpenStandaloneLootEditor()
    {
        if (onlyOneWindow && currentWindow != null)
        {
            currentWindow.Activate();
        }
        else
        {
            using var vm = creator();
            vm.LootType = LootSourceType.Creature;
            vm.SolutionEntry = 1;
            currentWindow = windowManager.ShowWindow(vm, out var task);
            if (vm.IsPerEntityLootEditingMode) // in the per table editing mode, it will open default empty loot by default
                vm.LoadLootCommand.Execute(null);
            await WindowLifetimeTask(currentWindow, task);
        }
    }

    public void OpenStandaloneLootEditor(LootSourceType type, uint solutionEntry, uint difficultyId)
    {
        if (typedWindows.TryGetValue((type, solutionEntry), out var window))
        {
            window.Activate();
        }
        else
        {
            using var vm = creator();
            vm.LootType = type;
            vm.SolutionEntry = solutionEntry;
            if (currentCoreVersion.Current.LootEditingMode == LootEditingMode.PerLogicalEntity)
            {
                vm.CanChangeLootType = false;
                vm.CanChangeEntry = false;
            }
            typedWindows[(type, solutionEntry)] = window = windowManager.ShowWindow(vm, out var task);
            vm.LoadLootCommand.Execute(null);
            WindowLifetimeTask(window, type, solutionEntry, task).ListenErrors();
        }
    }

    private async Task WindowLifetimeTask(IAbstractWindowView window, LootSourceType type, uint solutionEntry, Task lifetime)
    {
        await lifetime;
        typedWindows.Remove((type, solutionEntry));
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