using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseEntityMangosUnitConditionsChangedHistoryAction : IHistoryAction
    {
        private readonly DatabaseEntity entity;
        private readonly ColumnFullName column;
        private readonly MangosUnitConditionChange? oldChange;
        private readonly MangosUnitConditionChange? newChange;
        private readonly DatabaseKey actualKey;

        public DatabaseEntityMangosUnitConditionsChangedHistoryAction(DatabaseEntity entity,
            ColumnFullName column,
            MangosUnitConditionChange? oldChange,
            MangosUnitConditionChange? newChange,
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
            entity.SetMangosUnitConditions(column, oldChange);
        }

        public void Redo()
        {
            entity.SetMangosUnitConditions(column, newChange);
        }

        public string GetDescription()
        {
            return $"Entity {actualKey} unit condition ({column}) changed";
        }
    }
}
