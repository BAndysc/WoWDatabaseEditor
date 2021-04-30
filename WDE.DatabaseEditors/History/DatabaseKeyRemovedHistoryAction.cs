using WDE.Common.History;
using WDE.DatabaseEditors.ViewModels.MultiRow;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseKeyRemovedHistoryAction : IHistoryAction
    {
        private readonly MultiRowDbTableEditorViewModel viewModel;
        private readonly uint entity;

        public DatabaseKeyRemovedHistoryAction(MultiRowDbTableEditorViewModel viewModel,
            uint entity)
        {
            this.viewModel = viewModel;
            this.entity = entity;
        }
        
        public void Undo()
        {
            viewModel.DoAddKey(entity);
        }

        public void Redo()
        {
            viewModel.UndoAddKey(entity);
        }

        public string GetDescription()
        {
            return $"Removed key {entity}";
        }
    }
}