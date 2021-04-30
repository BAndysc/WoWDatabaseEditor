using WDE.Common.History;
using WDE.DatabaseEditors.ViewModels.MultiRow;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseKeyAddedHistoryAction : IHistoryAction
    {
        private readonly MultiRowDbTableEditorViewModel viewModel;
        private readonly uint entity;

        public DatabaseKeyAddedHistoryAction(MultiRowDbTableEditorViewModel viewModel,
            uint entity)
        {
            this.viewModel = viewModel;
            this.entity = entity;
        }
        
        public void Undo()
        {
            viewModel.UndoAddKey(entity);
        }

        public void Redo()
        {
            viewModel.DoAddKey(entity);
        }

        public string GetDescription()
        {
            return $"Added key {entity}";
        }
    }
}