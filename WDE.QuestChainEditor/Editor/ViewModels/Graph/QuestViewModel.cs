using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class QuestViewModel : ElementViewModel
    {
        public readonly Quest quest;

        public uint Id => quest.Id;

        public QuestViewModel(Quest quest) : base(quest.Name)
        {
            AddInputConnector("", Colors.Aqua);
            AddOutputConnector("", Colors.Aqua);
            this.quest = quest;
        }
        
    }
}
