using WDE.Module.Attributes;
using WDE.QuestChainEditor.Editor.ViewModels;
using WDE.QuestChainEditor.Editor.Views;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Providers
{
    [AutoRegister]
    public class QuestPicker : IQuestPicker
    {
        public QuestPicker(IQuestsProvider questsProvider)
        {
            QuestsProvider = questsProvider;
        }

        public IQuestsProvider QuestsProvider { get; }

        public QuestDefinition ChooseQuest()
        {
            QuestPickerWindow picker = new();
            QuestsViewModel model = new(QuestsProvider);
            picker.DataContext = model;

            bool? result = picker.ShowDialogCenteredToMouse();

            if (result.HasValue && result.Value && model.ChosenItem != null)
                return model.ChosenItem;
            return null;
        }
    }
}