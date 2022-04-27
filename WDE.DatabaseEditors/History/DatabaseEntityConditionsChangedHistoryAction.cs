using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseEntityConditionsChangedHistoryAction : IHistoryAction
    {
        private readonly DatabaseEntity entity;
        private readonly IReadOnlyList<ICondition>? oldConditions;
        private readonly IReadOnlyList<ICondition>? newConditions;
        private readonly ViewModelBase viewModel;
        private readonly DatabaseKey actualKey;

        public DatabaseEntityConditionsChangedHistoryAction(DatabaseEntity entity,
            IReadOnlyList<ICondition>? oldConditions, 
            IReadOnlyList<ICondition>? newConditions,
            ViewModelBase viewModel)
        {
            this.entity = entity;
            this.oldConditions = oldConditions;
            this.newConditions = newConditions;
            this.viewModel = viewModel;
            this.actualKey = entity.GenerateKey(viewModel.TableDefinition);
        }
        
        public void Undo()
        {
            entity.Conditions = oldConditions;
        }

        public void Redo()
        {
            entity.Conditions = newConditions;
        }

        public string GetDescription()
        {
            return $"Entity {actualKey} conditions changed";
        }
    }
}