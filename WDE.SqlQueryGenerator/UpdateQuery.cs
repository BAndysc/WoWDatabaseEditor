using System.Collections.Generic;

namespace WDE.SqlQueryGenerator
{
    internal class UpdateQuery : IUpdateQuery
    {
        private readonly IWhere condition;

        public UpdateQuery(IWhere condition)
        {
            this.condition = condition;
        }
        
        public UpdateQuery(IWhere condition, string key, string value)
        {
            this.condition = condition;
            updates.Add((key, value));
        }

        public UpdateQuery(IUpdateQuery update, string key, string value)
        {
            this.condition = update.Condition;
            updates.AddRange(update.Updates);
            updates.Add((key, value));
        }
        
        private List<(string, string)> updates = new();
        public IWhere Condition => condition;
        public IEnumerable<(string, string)> Updates => updates;
    }
}