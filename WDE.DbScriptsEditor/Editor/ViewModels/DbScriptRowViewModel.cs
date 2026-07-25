using WDE.DbScriptsEditor.Models;
using WDE.MVVM;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // Base of every editor row VM (action / wait / comment). Rows are EQUAL entities: all are
    // selectable, reorderable, copyable and deletable the same way.
    public abstract class DbScriptRowViewModel : ObservableBase
    {
        public DbScriptRow Row { get; }

        protected DbScriptRowViewModel(DbScriptRow row)
        {
            Row = row;
        }

        private bool isSelected;
        public bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }

        // True membership in an if block (an unbroken InIf run directly after an if row) —
        // renders as indentation. Recomputed by the editor from the model's membership walk.
        private bool inConditionBlock;
        public bool InConditionBlock { get => inConditionBlock; set => SetProperty(ref inConditionBlock, value); }
    }
}
