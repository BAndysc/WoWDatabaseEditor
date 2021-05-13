using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Expressions.Antlr;
using Antlr4.Runtime;
using WDE.Common.Database;

namespace WDE.DatabaseEditors.Expressions
{
    public class DatabaseExpressionEvaluator
    {
        private DatabaseEditorExpressionLexer lexer;
        private CommonTokenStream tokens;
        private DatabaseEditorExpressionParser parser;
        private ExpressionVisitor visitor;
        
        public DatabaseExpressionEvaluator(ICreatureStatCalculatorService statCalculatorService, string expression)
        {
            lexer = new DatabaseEditorExpressionLexer(new AntlrInputStream(expression));
            tokens = new CommonTokenStream(lexer);
            parser = new DatabaseEditorExpressionParser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();

            visitor = new ExpressionVisitor(statCalculatorService);
        }

        public object? Evaluate(DatabaseEntity entity)
        {
            lexer.Reset();
            tokens.Reset();
            parser.Reset();
            visitor.SetContext(entity);
            return visitor.Visit(parser.expr());
        }
    }
}