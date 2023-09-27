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
        
        public UpdateQuery(IWhere condition, string key, string value, string? comment, IUpdateQuery.Operator op)
        {
            this.condition = condition;
            updates.Add((key, value, op, comment));
        }

        public UpdateQuery(IUpdateQuery update, string key, string value, string? commment, IUpdateQuery.Operator op)
        {
            this.condition = update.Condition;
            updates.AddRange(update.Updates);
            updates.Add((key, value, op, commment));
        }
        
        private List<(string column, string value, IUpdateQuery.Operator op, string? comment)> updates = new();
        public IWhere Condition => condition;
        public IReadOnlyList<(string column, string value, IUpdateQuery.Operator op, string? comment)> Updates => updates;
    }
}