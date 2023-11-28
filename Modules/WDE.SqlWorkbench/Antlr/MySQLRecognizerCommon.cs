using System;
using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace WDE.SqlWorkbench.Antlr;

[Flags]
public enum SqlModeEnum
{
    NoMode = 0,
    AnsiQuotes = 1 << 0,
    HighNotPrecedence = 1 << 1,
    PipesAsConcat = 1 << 2,
    IgnoreSpace = 1 << 3,
    NoBackslashEscapes = 1 << 4
}

// this is based on https://github.com/mysql/mysql-workbench/tree/8.0/library/parsers/mysql
// don't rename things here!
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class MySQLRecognizerCommon
{
    public long serverVersion;
    public SqlModeEnum sqlMode;

    public bool isSqlModeActive(SqlModeEnum mode)
    {
        return (sqlMode & mode) != 0;
    }

    public void sqlModeFromString(string modes)
    {
        sqlMode = SqlModeEnum.NoMode;
        modes = modes.ToUpper();
        var modeList = modes.Split(',');

        foreach (var mode in modeList)
        {
            var trimmedMode = mode.Trim();
            switch (trimmedMode)
            {
                case "ANSI":
                case "DB2":
                case "MAXDB":
                case "MSSQL":
                case "ORACLE":
                case "POSTGRESQL":
                    sqlMode |= SqlModeEnum.AnsiQuotes | SqlModeEnum.PipesAsConcat | SqlModeEnum.IgnoreSpace;
                    break;
                case "ANSI_QUOTES":
                    sqlMode |= SqlModeEnum.AnsiQuotes;
                    break;
                case "PIPES_AS_CONCAT":
                    sqlMode |= SqlModeEnum.PipesAsConcat;
                    break;
                case "NO_BACKSLASH_ESCAPES":
                    sqlMode |= SqlModeEnum.NoBackslashEscapes;
                    break;
                case "IGNORE_SPACE":
                    sqlMode |= SqlModeEnum.IgnoreSpace;
                    break;
                case "HIGH_NOT_PRECEDENCE":
                case "MYSQL323":
                case "MYSQL40":
                    sqlMode |= SqlModeEnum.HighNotPrecedence;
                    break;
            }
        }
    }

    // public static string DumpTree(ParserRuleContext context, IVocabulary vocabulary)
    // {
    //     StringWriter writer = new StringWriter();
    //     foreach (var child in context.children)
    //     {
    //         if (child is ParserRuleContext ruleContext)
    //         {
    //             writer.WriteLine(ruleContext.ToStringTree((Vocabulary)vocabulary));
    //         }
    //         else if (child is ITerminalNode terminalNode)
    //         {
    //             writer.WriteLine(terminalNode.GetText());
    //         }
    //     }
    //     return writer.ToString();
    // }

    public static string sourceTextForContext(ParserRuleContext ctx, bool keepQuotes = false)
    {
        return sourceTextForRange(ctx.Start, ctx.Stop, keepQuotes);
    }

    public static string sourceTextForRange(IToken start, IToken stop, bool keepQuotes = false)
    {
        ICharStream stream = start.TokenSource.InputStream;
        string text = stream.GetText(Interval.Of(start.StartIndex, stop.StopIndex));

        if (keepQuotes || text.Length < 2)
            return text;

        char quoteChar = text[0];
        if ((quoteChar == '"' || quoteChar == '`' || quoteChar == '\'') && quoteChar == text[^1])
        {
            if (quoteChar == '"' || quoteChar == '\'')
                text = text.Replace(new string(quoteChar, 2), quoteChar.ToString());
            return text[1..^1];
        }

        return text;
    }

    public static IParseTree? getPrevious(IParseTree tree)
    {
        if (tree.Parent == null)
            return null;

        for (int i = 0; i < tree.Parent.ChildCount; ++i)
        {
            if (tree.Parent.GetChild(i) == tree)
            {
                if (i > 0)
                    return tree.Parent.GetChild(i - 1);
                return null;
            }
        }

        return null;
    }

    public static IParseTree? getNext(IParseTree tree)
    {
        if (tree.Parent == null)
            return null;

        for (int i = 0; i < tree.Parent.ChildCount; ++i)
        {
            if (tree.Parent.GetChild(i) == tree)
            {
                if (i < tree.Parent.ChildCount - 1)
                    return tree.Parent.GetChild(i + 1);
                return null;
            }
        }

        return null;
    }

    public static IParseTree? TerminalFromPosition(IParseTree root, Tuple<int, int> position)
    {
        IParseTree? current = root;
        while (current != null)
        {
            if (current is ITerminalNode terminalNode)
            {
                IToken token = terminalNode.Symbol;
                if (token.Line > position.Item1 || (token.Line == position.Item1 && token.Column + token.Text.Length >= position.Item2))
                    return terminalNode;
            }

            current = getNext(current);
        }
        return null;
    }

    public static IParseTree? contextFromPosition(IParseTree root, int position)
    {
        if (root is ITerminalNode terminalNode)
        {
            IToken token = terminalNode.Symbol;
            if (token.StartIndex <= position && position <= token.StopIndex)
                return root;
        }
        else if (root is ParserRuleContext context)
        {
            foreach (var child in context.children)
            {
                IParseTree? found = contextFromPosition(child, position);
                if (found != null)
                    return found;
            }
        }

        return null;
    }
}