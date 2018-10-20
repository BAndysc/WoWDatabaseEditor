using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Attributes;
using WDE.QuestChainEditor.Editor.ViewModels;
using WDE.QuestChainEditor.Editor.Views;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Providers
{
    [AutoRegister]
    public class QuestPicker : IQuestPicker
    {
        public IQuestsProvider QuestsProvider { get; }

        public QuestPicker(IQuestsProvider questsProvider)
        {
            QuestsProvider = questsProvider;
        }
        
        public QuestDefinition ChooseQuest()
        {
            var picker = new QuestPickerWindow();
            var model = new QuestsViewModel(QuestsProvider);
            picker.DataContext = model;
            
            var result = picker.ShowDialogCenteredToMouse();
            
            if (result.HasValue && result.Value && model.ChosenItem != null)
            {
                return model.ChosenItem;
            }
            return null;
        }
    }
}
