using WDE.Common.History;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.MultiRow;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseExecuteDeleteHistoryAction :  IHistoryAction
    {
        private readonly MultiRowDbTableEditorViewModel viewModel;
        private readonly DatabaseEntity entity;

        public DatabaseExecuteDeleteHistoryAction(MultiRowDbTableEditorViewModel viewModel,
            DatabaseEntity entity)
        {
            this.viewModel = viewModel;
            this.entity = entity;
        }
        
        public void Undo()
        {
        }

        public void Redo()
        {
            viewModel.RedoExecuteDelete(entity);
        }

        public string GetDescription()
        {
            return $"Executed DELETE query for {entity.Key}";
        }
    }
}