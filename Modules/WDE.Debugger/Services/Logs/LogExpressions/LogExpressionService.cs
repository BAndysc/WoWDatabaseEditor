using System;
using System.IO;
using Antlr4.Runtime;
using Newtonsoft.Json.Linq;
using WDE.Debugger.Services.Logs.LogExpressions.Antlr;
using WDE.Module.Attributes;

namespace WDE.Debugger.Services.Logs.LogExpressions;

[AutoRegister]
[SingleInstance]
internal class LogExpressionService : ILogExpressionService
{
    public LogExpressionService()
    {

    }

    public JToken Parse(JObject root, string expression)
    {
        var lexer = new LogExpressionLexer(new AntlrInputStream(expression));
        var tokens = new CommonTokenStream(lexer);
        var parser = new LogExpressionParser(tokens);
        parser.BuildParseTree = true;
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new CustomErrorListener());
        var visitor = new LogExpressionVisitor();
        visitor.SetRoot(root);
        return visitor.Visit(parser.expr());
    }

    public class CustomErrorListener : IAntlrErrorListener<IToken>
    {
        public void SyntaxError(TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            throw new Exception("Parse error in line " + line + " at position " + charPositionInLine + ": " + msg);
        }
    }
}