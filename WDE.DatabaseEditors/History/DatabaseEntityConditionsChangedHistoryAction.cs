using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseEntityConditionsChangedHistoryAction : IHistoryAction
    {
        private readonly DatabaseEntity entity;
        private readonly IReadOnlyList<ICondition>? oldConditions;
        private readonly IReadOnlyList<ICondition>? newConditions;

        public DatabaseEntityConditionsChangedHistoryAction(DatabaseEntity entity,
            IReadOnlyList<ICondition>? oldConditions, IReadOnlyList<ICondition>? newConditions)
        {
            this.entity = entity;
            this.oldConditions = oldConditions;
            this.newConditions = newConditions;
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
            return $"Entity {entity.Key} conditions changed";
        }
    }
}