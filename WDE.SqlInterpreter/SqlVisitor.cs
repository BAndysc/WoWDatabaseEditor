using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using WDE.Common.Database;
using WDE.Common.Services.QueryParser.Models;
using WDE.SqlInterpreter.Extensions;

namespace WDE.SqlInterpreter
{
    internal class SqlVisitor : MySqlParserBaseVisitor<bool>
    {
        private readonly string? worldTableName;
        private readonly string? hotfixTableName;
        private readonly DataDatabaseType defaultContext;
        private Dictionary<string, object> variables = new();

        public SqlVisitor(string? worldTableName, string? hotfixTableName, DataDatabaseType defaultContext)
        {
            this.worldTableName = worldTableName;
            this.hotfixTableName = hotfixTableName;
            this.defaultContext = defaultContext;
        }
        
        private string GetOriginalText(ParserRuleContext context)
        {
            return context.Start.InputStream.GetText(new Interval(context.Start.StartIndex, context.Stop.StopIndex));
        }
        
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

                    where = new WhereCondition(new EqualityWhereCondition(columns.ToArray(), values.ToArray(), GetOriginalText(logical)));
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
                        bcpc.right.GetText().ToType(variables), GetOriginalText(bcpc)));
                    return true;
                } else if (pec.predicate() is MySqlParser.InPredicateContext inc)
                {
                    if (inc.expressions() == null)
                        return false;

                    var subconditions = new EqualityWhereCondition[inc.expressions().expression().Length];
                    int index = 0;
                    foreach (var expr in inc.expressions().expression())
                    {
                        var value = expr.GetText().ToType(variables)!;
                        var subCondition = new EqualityWhereCondition(inc.predicate().GetText().DropQuotes()!,
                            value, GetOriginalText(inc));
                        subconditions[index++] = subCondition;
                    }
                    @where = new WhereCondition(subconditions);
                    return true;
                }
            }

            return false;
        }

        private bool ParseDatabaseTable(MySqlParser.UidContext[]? uids, out DatabaseTable tableName)
        {
            tableName = default;
            
            if (uids == null)
                return false;
            
            var tableNameId = uids[^1].GetText().DropQuotes();
            var databaseNameId = uids.Length >= 2 ? uids[^2].GetText().DropQuotes() : null;

            if (tableNameId == null)
                return false;

            if (databaseNameId == null)
            {
                tableName = new DatabaseTable(defaultContext, tableNameId);
                return true;
            }

            if (databaseNameId == worldTableName)
            {
                tableName = DatabaseTable.WorldTable(tableNameId);
                return true;
            }
            
            if (databaseNameId == hotfixTableName)
            {
                tableName = DatabaseTable.HotfixTable(tableNameId);
                return true;
            }

            return false;
        }
        
        public override bool VisitDeleteStatement(MySqlParser.DeleteStatementContext context)
        {
            if (context.singleDeleteStatement() == null)
                return false;

            var identifiers = context.singleDeleteStatement().tableName().fullId().uid();

            if (!ParseDatabaseTable(identifiers, out var tableName))
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

            var identifiers = context.singleUpdateStatement().tableName().fullId().uid();
            
            if (!ParseDatabaseTable(identifiers, out var tableName))
                return false;
            
            if (context.singleUpdateStatement().updatedElement().Length == 0)
                return false;

            if (!TryParseSimpleCondition(context.singleUpdateStatement().expression(), out var where))
                return false;
            
            var updates = context.singleUpdateStatement().updatedElement()
                .Select(upd => new UpdateElement(upd.fullColumnName().GetText().DropQuotes()!,
                    upd.expression().GetText().ToType(variables)))
                .ToList();
            
            Queries.Add(new UpdateQuery(tableName, updates, where));
            
            return base.VisitUpdateStatement(context);
        }
        
        public override bool VisitInsertStatement(MySqlParser.InsertStatementContext context)
        {
            var identifiers = context.tableName().fullId().uid();
            
            if (!ParseDatabaseTable(identifiers, out var tableName))
                return false;
            
            if (context.columns == null)
                return false;
            
            var columns = context.columns.uid().Select(col => col.GetText().DropQuotes()!).ToList();

            var inserts = context.insertStatementValue().expressionsWithDefaults()
                .Where(line => line.expressionOrDefault().Length == columns.Count)
                .Select(line =>
            {
                return (IReadOnlyList<object>)line.expressionOrDefault().Select(val => val.GetText().ToType(variables)).ToList();
            }).ToList();
            if (inserts.Count > 0)
                Queries.Add(new InsertQuery(tableName, columns, inserts));
            return base.VisitInsertStatement(context);
        }

        public override bool VisitSetVariable(MySqlParser.SetVariableContext context)
        {
            for (int i = 0; i < context.expression().Length; ++i)
            {
                var variableName = context.variableClause(i).GetText();
                var expression = GetOriginalText(context.expression(i)).ToType(variables);
                if (expression != null)
                    variables[variableName] = expression;
            }
            return true;
        }

        public IEnumerable<InsertQuery> Inserts => Queries.Where(q => q is InsertQuery).Cast<InsertQuery>();
        public IEnumerable<UpdateQuery> Updates => Queries.Where(q => q is UpdateQuery).Cast<UpdateQuery>();
        public IEnumerable<DeleteQuery> Deletes => Queries.Where(q => q is DeleteQuery).Cast<DeleteQuery>();
        public List<IBaseQuery> Queries { get; } = new List<IBaseQuery>();
    }
}