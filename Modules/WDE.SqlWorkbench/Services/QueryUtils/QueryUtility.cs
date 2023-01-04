using System;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Antlr;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.QueryUtils;

[AutoRegister]
[SingleInstance]
internal class QueryUtility : IQueryUtility
{
    private NoCopyStringCharStream inputStream;
    private MySQLLexer lexer;
    private CommonTokenStream tokens;
    private MySQLParser parser;
    private MySQLParserBaseListener listener;

    private static readonly System.Type[] AllowedSelectChildrenNodes = new[]
    {
        typeof(TerminalNodeImpl),
        typeof(MySQLParser.SelectItemListContext),
        typeof(MySQLParser.FromClauseContext),
        typeof(MySQLParser.WhereClauseContext)
    };
    
    public QueryUtility()
    {
        inputStream = new NoCopyStringCharStream("");
        lexer = new MySQLLexer(inputStream);
        tokens = new CommonTokenStream(lexer);
        parser = new MySQLParser(tokens);
        listener = new MySQLParserBaseListener();
        lexer.RemoveErrorListeners();
        parser.RemoveErrorListeners();
    }

    private bool IsSimpleColumnId(MySQLParser.SelectItemContext selectItem)
    {
        if (selectItem.ChildCount != 1)
            return false;
        
        if (selectItem.tableWild() != null && selectItem.expr() == null)
            return true;

        if (selectItem.expr() == null)
            return false;

        if (selectItem.expr().children.Count != 1)
            return false;

        if (selectItem.expr().children[0] is not MySQLParser.PrimaryExprPredicateContext primaryExprPredicate)
            return false;

        if (primaryExprPredicate.children.Count != 1)
            return false;

        if (primaryExprPredicate.children[0] is not MySQLParser.PredicateContext predicate)
            return false;

        if (predicate.children.Count != 1)
            return false;

        if (predicate.children[0] is not MySQLParser.BitExprContext bitExpr)
            return false;
        
        if (bitExpr.children.Count != 1)
            return false;
        
        if (bitExpr.children[0] is not MySQLParser.SimpleExprColumnRefContext simpleExpr)
            return false;

        if (simpleExpr.children.Count != 1)
            return false;

        if (simpleExpr.children[0] is not MySQLParser.ColumnRefContext columnRef)
            return false;

        return true;
    }

    public bool TryGetFrom(MySQLParser.TableReferenceListContext context,
        out string? fromSchema,
        out string fromTable)
    {
        fromSchema = null;
        fromTable = null!;
        
        if (context.children.Count != 1 ||
            context.children[0] is not MySQLParser.TableReferenceContext tableReference)
            return false;
        
        if (tableReference.ChildCount != 1 ||
            tableReference.children[0] is not MySQLParser.TableFactorContext tableFactor)
            return false;
        
        if (tableFactor.ChildCount != 1 ||
            tableFactor.children[0] is not MySQLParser.SingleTableContext singleTable)
            return false;
        
        if (singleTable.ChildCount != 1 ||
            singleTable.children[0] is not MySQLParser.TableRefContext tableRef)
            return false;
        
        if (tableRef.ChildCount != 1 ||
            tableRef.children[0] is not MySQLParser.QualifiedIdentifierContext qualifiedIdentifier)
            return false;

        if (qualifiedIdentifier.ChildCount < 1 || qualifiedIdentifier.ChildCount > 2)
            return false;

        var schemaContext = qualifiedIdentifier.ChildCount >= 2 ? qualifiedIdentifier.children[^2] : null;
        var tableContext = qualifiedIdentifier.children[^1];

        bool TryExtractIdentifier(IParseTree tree, out string text)
        {
            if (tree is MySQLParser.IdentifierContext identifierContext)
            {
                text = identifierContext.pureIdentifier().GetText();
                if (text.StartsWith('`'))
                    text = text[1..^1];
                return true;
            }
            else if (tree is MySQLParser.DotIdentifierContext dotIdentifierContext)
            {
                return TryExtractIdentifier(dotIdentifierContext.identifier(), out text);
            }

            text = null!;
            return false;
        }
        
        if (schemaContext != null && !TryExtractIdentifier(schemaContext, out fromSchema))
            return false;
        
        if (!TryExtractIdentifier(tableContext, out fromTable))
            return false;
        
        return true;
    }
    
    public bool IsSimpleSelect(string query, out SimpleSelect simpleSelect)
    {
        simpleSelect = default;
        try
        {
            inputStream.Reset(query);
            lexer.SetInputStream(inputStream);
            tokens.SetTokenSource(lexer);
            parser.TokenStream = tokens;
            parser.Reset();

            var queryExpression = parser
                .query()
                ?.simpleStatement()
                ?.selectStatement()
                ?.queryExpression();

            if (queryExpression == null)
                return false;

            var body = queryExpression.queryExpressionBody();

            var selectPrimary = body?.queryPrimary();
            var union = body?.unionOption();

            if (selectPrimary == null || selectPrimary.Length != 1)
                return false;

            if (!(union == null || union.Length == 0))
                return false;

            if (selectPrimary[0].querySpecification() is not { } select)
                return false;

            if (select.selectOption().Length != 0) // i.e. distinct
                return false;

            if (select.groupByClause() != null)
                return false;

            if (select.havingClause() != null)
                return false;

            if (select.intoClause() != null)
                return false;

            if (select.fromClause() == null)
                return false;

            if (select.fromClause().DUAL_SYMBOL() != null)
                return false;

            if (select.fromClause().tableReferenceList().ChildCount != 1)
                return false;

            if (select.fromClause().tableReferenceList().children[0] is not MySQLParser.TableReferenceContext
                tableReference)
                return false;

            if (tableReference.ChildCount != 1)
                return false;

            if (tableReference.children[0] is not MySQLParser.TableFactorContext tableFactor)
                return false;

            if (tableFactor.singleTable() is not { } fromTable)
                return false;

            if (fromTable.ChildCount != 1 || fromTable.tableRef() == null)
                return false;

            if (select.selectItemList() == null)
                return false;

            if (select.selectItemList().selectItem().Any(x => !IsSimpleColumnId(x)))
                return false;

            // not necessary, but in case the grammar changes, this is a good sanity check.
            // it is better when this function reports some false negatives than false positives
            if (select.children.Any(child => !AllowedSelectChildrenNodes.Contains(child.GetType())))
                return false;

            string? GetText(ParserRuleContext? node)
            {
                if (node == null)
                    return null;
                return inputStream.GetText(new Interval(node.Start.StartIndex, node.Stop.StopIndex));
            }

            TryGetFrom(select.fromClause().tableReferenceList(), out var fromSchema, out var table);
            var columns = GetText(select.selectItemList())!;
            var where = GetText(select.whereClause()?.expr());
            var order = GetText(queryExpression.orderClause()?.orderList());
            var limit = GetText(queryExpression.limitClause()?.limitOptions());

            simpleSelect = new SimpleSelect(columns, new SimpleFrom(fromSchema, table), where, order, limit);

            return true;
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return false;
        }
    }

    public bool IsUseDatabase(string query, out string databaseName)
    {
        var useRegex = new System.Text.RegularExpressions.Regex(@"^\s*use\s+`?([^`]+)`?\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var match = useRegex.Match(query);
        if (match.Success)
        {
            databaseName = match.Groups[1].Value;
            return true;
        }
        databaseName = "";
        return false;
    }
}