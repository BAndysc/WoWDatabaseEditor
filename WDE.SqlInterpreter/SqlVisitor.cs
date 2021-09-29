using System.Collections.Generic;
using System.Linq;
using WDE.SqlInterpreter.Extensions;
using WDE.SqlInterpreter.Models;

namespace WDE.SqlInterpreter
{
    internal class SqlVisitor : MySqlParserBaseVisitor<bool>
    {
        public override bool VisitUpdateStatement(MySqlParser.UpdateStatementContext context)
        {
            if (context.singleUpdateStatement() == null)
                return false;
            
            var tableName = context.singleUpdateStatement().tableName().fullId().uid()[^1].GetText().DropQuotes();
            
            if (tableName == null)
                return false;

            if (context.singleUpdateStatement().updatedElement().Length == 0)
                return false;

            if (context.singleUpdateStatement().expression() is not MySqlParser.PredicateExpressionContext pec)
                return false;

            SimpleWhereCondition where;
            if (pec.predicate() is MySqlParser.BinaryComparisonPredicateContext bcpc)
            {
                if (bcpc.comparisonOperator().GetText() != "=")
                    return false;

                @where = new SimpleWhereCondition(bcpc.left.GetText().DropQuotes()!,
                    new List<object?>() { bcpc.right.GetText().ToType() });
            } else if (pec.predicate() is MySqlParser.InPredicateContext inc)
            {
                if (inc.expressions() == null)
                    return false;
                
                @where = new SimpleWhereCondition(inc.predicate().GetText().DropQuotes()!,
                     inc.expressions().expression().Select(e => e.GetText().ToType()!).ToList());
            }
            else
                return false;

            var updates = context.singleUpdateStatement().updatedElement()
                .Select(upd => new UpdateElement(upd.fullColumnName().GetText().DropQuotes()!))
                .ToList();
            
            Queries.Add(new UpdateQuery(tableName, updates, where));
            
            return base.VisitUpdateStatement(context);
        }
        
        public override bool VisitInsertStatement(MySqlParser.InsertStatementContext context)
        {
            var tableName = context.tableName().fullId().uid()[^1].GetText().DropQuotes();

            if (tableName == null)
                return false;

            if (context.columns == null)
                return false;
            
            var columns = context.columns.uid().Select(col => col.GetText().DropQuotes()!).ToList();

            var inserts = context.insertStatementValue().expressionsWithDefaults()
                .Where(line => line.expressionOrDefault().Length == columns.Count)
                .Select(line =>
            {
                return (IReadOnlyList<object>)line.expressionOrDefault().Select(val => val.GetText().ToType()).ToList();
            }).ToList();
            if (inserts.Count > 0)
                Queries.Add(new InsertQuery(tableName, columns, inserts));
            return base.VisitInsertStatement(context);
        }

        public IEnumerable<InsertQuery> Inserts => Queries.Where(q => q is InsertQuery).Cast<InsertQuery>();
        public IEnumerable<UpdateQuery> Updates => Queries.Where(q => q is UpdateQuery).Cast<UpdateQuery>();
        public List<IBaseQuery> Queries { get; } = new List<IBaseQuery>();
    }
}