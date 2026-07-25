using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseEntityMangosConditionsChangedHistoryAction : IHistoryAction
    {
        private readonly DatabaseEntity entity;
        private readonly ColumnFullName column;
        private readonly MangosConditionsChange? oldChange;
        private readonly MangosConditionsChange? newChange;
        private readonly DatabaseKey actualKey;

        public DatabaseEntityMangosConditionsChangedHistoryAction(DatabaseEntity entity,
            ColumnFullName column,
            MangosConditionsChange? oldChange,
            MangosConditionsChange? newChange,
            ViewModelBase viewModel)
        {
            this.entity = entity;
            this.column = column;
            this.oldChange = oldChange;
            this.newChange = newChange;
            this.actualKey = entity.GenerateKey(viewModel.TableDefinition);
        }

        public void Undo()
        {
            entity.SetMangosConditions(column, oldChange);
        }

        public void Redo()
        {
            entity.SetMangosConditions(column, newChange);
        }

        public string GetDescription()
        {
            return $"Entity {actualKey} conditions ({column}) changed";
        }
    }
}
