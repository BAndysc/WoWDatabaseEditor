using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.MultiRow;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseExecuteDeleteHistoryAction :  IHistoryAction
    {
        private readonly MultiRowDbTableEditorViewModel viewModel;
        private readonly DatabaseEntity entity;
        private readonly DatabaseKey actualKey;

        public DatabaseExecuteDeleteHistoryAction(MultiRowDbTableEditorViewModel viewModel,
            DatabaseEntity entity)
        {
            this.viewModel = viewModel;
            this.entity = entity;
            actualKey = entity.GenerateKey(viewModel.TableDefinition);
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
            return $"Executed DELETE query for {actualKey}";
        }
    }
}