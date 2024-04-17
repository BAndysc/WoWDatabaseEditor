using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.ViewModels;

[AutoRegister]
[SingleInstance]
public class QuestPickerService
{
    private readonly IWindowManager windowManager;
    private readonly Func<QuestPickerViewModel> questPickerViewModelFactory;
    private QuestPickerViewModel? questPickerViewModel;

    private QuestPickerViewModel ViewModel
    {
        get
        {
            if (questPickerViewModel != null)
                return questPickerViewModel;
            questPickerViewModel = questPickerViewModelFactory();
            return questPickerViewModel;
        }
    }

    public QuestPickerService(IWindowManager windowManager, Func<QuestPickerViewModel> questPickerViewModelFactory)
    {
        this.windowManager = windowManager;
        this.questPickerViewModelFactory = questPickerViewModelFactory;
    }

    public async Task<uint?> PickQuest()
    {
        ViewModel.Reset();
        var result = await windowManager.ShowDialog(ViewModel);
        if (!result)
            return null;
        return ViewModel.SelectedQuest;
    }
}