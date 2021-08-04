using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Editor.ViewModels
{
    public class QuestViewModel : ElementViewModel
    {
        public readonly Quest Quest;

        public QuestViewModel(Quest quest) : base(quest.Name)
        {
            AddInputConnector("", Colors.Aqua);
            AddOutputConnector("", Colors.Aqua);
            Quest = quest;
        }

        public uint Id => Quest.Id;
    }
}