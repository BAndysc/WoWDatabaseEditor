using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Editor.DisignTimeViewModels
{
    internal class QuestPickerViewModel
    {
        public QuestPickerViewModel()
        {
            Items = new CollectionViewSource();
            Items.Source = allItems;
        }

        private CollectionViewSource Items { get; }

        public ICollectionView AllItems => Items.View;

        private IEnumerable<QuestDefinition> allItems =>
            new[]
            {
                new QuestDefinition(26058, "In Defense of Krom'gar Fortress"),
                new QuestDefinition(26048, "Spare Parts Up In Here!"),
                new QuestDefinition(26047, "And That's Why They Call Them Peons...")
            };
    }
}