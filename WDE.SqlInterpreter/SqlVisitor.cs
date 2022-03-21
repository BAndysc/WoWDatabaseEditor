using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WDE.SqlInterpreter.Extensions;
using WDE.SqlInterpreter.Models;

namespace WDE.SqlInterpreter
{
    internal class SqlVisitor : MySqlParserBaseVisitor<bool>
    {
        private bool TryParseSimpleCondition(MySqlParser.ExpressionContext context, out WhereCondition where)
        {
            where = null!;

            if (context is MySqlParser.PredicateExpressionContext pred &&
                pred.predicate() is MySqlParser.ExpressionAtomPredicateContext ePred &&
                ePred.expressionAtom() is MySqlParser.NestedExpressionAtomContext atom &&
                atom.expression().Length == 1)
                return TryParseSimpleCondition(atom.expression()[0], out where);
            
            if (context is MySqlParser.LogicalExpressionContext logical)
            {
                if (logical.logicalOperator().GetText() == "OR")
                {
                    List<EqualityWhereCondition> conditions = new List<EqualityWhereCondition>();
                    foreach (var expression in logical.expression())
                    {
                        if (!TryParseSimpleCondition(expression, out var subWhere))
                            return false;

                        conditions.AddRange(subWhere.Conditions);
                    }
                    where = new WhereCondition(conditions.ToArray());
                    return true;
                }
                else if (logical.logicalOperator().GetText() == "AND")
                {
                    List<string> columns = new();
                    List<object?> values = new();
                    foreach (var expr in logical.expression())
                    {
                        if (!TryParseSimpleCondition(expr, out var subWhere))
                            return false;

                        if (subWhere.Conditions.Length != 1)
                            return false;

                        if (subWhere.Conditions[0].Columns.Length != 1)
                            return false;
                        
                        columns.Add(subWhere.Conditions[0].Columns[0]);
                        values.Add(subWhere.Conditions[0].Values[0]);
                    }

                    where = new WhereCondition(new EqualityWhereCondition(columns.ToArray(), values.ToArray()));
                    return true;
                }
            }
            else if (context is MySqlParser.PredicateExpressionContext pec)
            {
                if (pec.predicate() is MySqlParser.BinaryComparisonPredicateContext bcpc)
                {
                    if (bcpc.comparisonOperator().GetText() != "=")
                        return false;

                    @where = new WhereCondition(new EqualityWhereCondition(bcpc.left.GetText().DropQuotes()!,
                        bcpc.right.GetText().ToType()));
                    return true;
                } else if (pec.predicate() is MySqlParser.InPredicateContext inc)
                {
                    if (inc.expressions() == null)
                        return false;

                    var subconditions = new EqualityWhereCondition[inc.expressions().expression().Length];
                    int index = 0;
                    foreach (var expr in inc.expressions().expression())
                    {
                        var value = expr.GetText().ToType()!;
                        var subCondition = new EqualityWhereCondition(inc.predicate().GetText().DropQuotes()!,
                            value);
                        subconditions[index++] = subCondition;
                    }
                    @where = new WhereCondition(subconditions);
                    return true;
                }
            }

            return false;
        }

        public override bool VisitDeleteStatement(MySqlParser.DeleteStatementContext context)
        {
            if (context.singleDeleteStatement() == null)
                return false;
            
            var tableName = context.singleDeleteStatement().tableName().fullId().uid()[^1].GetText().DropQuotes();
            
            if (tableName == null)
                return false;

            if (!TryParseSimpleCondition(context.singleDeleteStatement().expression(), out var where))
                return false;
            
            Queries.Add(new DeleteQuery(tableName, where));
            
            return base.VisitDeleteStatement(context);
        }

        public override bool VisitUpdateStatement(MySqlParser.UpdateStatementContext context)
        {
            if (context.singleUpdateStatement() == null)
                return false;
            
            var tableName = context.singleUpdateStatement().tableName().fullId().uid()[^1].GetText().DropQuotes();
            
            if (tableName == null)
                return false;

            if (context.singleUpdateStatement().updatedElement().Length == 0)
                return false;

            if (!TryParseSimpleCondition(context.singleUpdateStatement().expression(), out var where))
                return false;
            
            var updates = context.singleUpdateStatement().updatedElement()
                .Select(upd => new UpdateElement(upd.fullColumnName().GetText().DropQuotes()!,
                    upd.expression().GetText()))
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
        public IEnumerable<DeleteQuery> Deletes => Queries.Where(q => q is DeleteQuery).Cast<DeleteQuery>();
        public List<IBaseQuery> Queries { get; } = new List<IBaseQuery>();
    }
}