using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Editor.DisignTimeViewModels
{
    internal class QuestPickerViewModel
    {
        private CollectionViewSource items { get; }

        public ICollectionView AllItems => items.View;

        private IEnumerable<QuestDefinition> _allItems =>
            new[]
            {
                new QuestDefinition(26058, "In Defense of Krom'gar Fortress"),
                new QuestDefinition(26048, "Spare Parts Up In Here!"),
                new QuestDefinition(26047, "And That's Why They Call Them Peons...")
            };

        public QuestPickerViewModel()
        {
            items = new CollectionViewSource();
            items.Source = _allItems;
        }
    }
}
