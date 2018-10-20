using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class QuestChainEditorViewModel : BindableBase
    {
        public GraphViewModel GraphViewModel { get; }
        
        public QuestChainEditorViewModel(IQuestPicker picker, QuestList quests)
        {
            GraphViewModel = new GraphViewModel(picker, quests);
            //GraphViewModel.AddElement(new NodeViewModel("Spare Parts Up In Here!"), 10000, 10000);
            //GraphViewModel.AddElement(new NodeViewModel("In Defense of Krom'gar Fortress"), 10100, 10000);
            //GraphViewModel.AddElement(new NodeViewModel("Eyes and Ears: Malaka'jin"), 10200, 10000);
            //GraphViewModel.AddElement(new NodeViewModel("Da Voodoo: Stormer Heart"), 10300, 10000);
            //GraphViewModel.AddElement(new NodeViewModel("Da Voodoo: Ram Horns"), 10300, 10000);
            //GraphViewModel.AddElement(new NodeViewModel("Da Voodoo: Resonite Crystal"), 10300, 10000);

        }
    }
}
