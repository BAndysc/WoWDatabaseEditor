using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Antlr4.Runtime;

namespace WDE.SqlWorkbench.Antlr;

// this is based on https://github.com/mysql/mysql-workbench/tree/8.0/library/parsers/mysql
// don't rename things here!
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public abstract class MySQLBaseRecognizer : Parser //, IMySQLRecognizerCommon
{
    private MySQLRecognizerCommon common = new(); // Replacing multiple inheritance

    public long serverVersion => common.serverVersion;
    public SqlModeEnum sqlMode => common.sqlMode;

    public bool isSqlModeActive(SqlModeEnum mode) => common.isSqlModeActive(mode);
    public void sqlModeFromString(string modes) => common.sqlModeFromString(modes);
    public static SqlModeEnum NoMode => SqlModeEnum.NoMode;
    public static SqlModeEnum AnsiQuotes => SqlModeEnum.AnsiQuotes;
    public static SqlModeEnum HighNotPrecedence => SqlModeEnum.HighNotPrecedence;
    public static SqlModeEnum PipesAsConcat => SqlModeEnum.PipesAsConcat;
    public static SqlModeEnum IgnoreSpace => SqlModeEnum.IgnoreSpace;
    public static SqlModeEnum NoBackslashEscapes => SqlModeEnum.NoBackslashEscapes;

    public MySQLBaseRecognizer(ITokenStream input) : base(input)
    {
        RemoveErrorListeners();
    }

    public MySQLBaseRecognizer(ITokenStream input, TextWriter output, TextWriter errorOutput)
        : base(input, output, errorOutput)
    {
        RemoveErrorListeners();
    }
    
    public static string GetText(ParserRuleContext context, bool convertEscapes)
    {
        if (context is MySQLParser.TextLiteralContext textLiteralContext)
        {
            // TODO: take the optional repertoire prefix into account.
            var result = string.Empty;
            var list = textLiteralContext.textStringLiteral();

            int lastType = TokenConstants.InvalidType;
            int lastIndex = Int32.MaxValue; // TokenConstants.InvalidIndex;
            foreach (var entry in list)
            {
                var token = entry.value;
                switch (token.Type)
                {
                    case MySQLParser.DOUBLE_QUOTED_TEXT:
                    case MySQLParser.SINGLE_QUOTED_TEXT:
                    {
                        var text = token.Text;
                        var quoteChar = text[0];
                        var doubledQuoteChar = new string(quoteChar, 2);

                        if (lastType == token.Type && lastIndex + 1 == token.TokenIndex)
                            result += quoteChar;
                        lastType = token.Type;
                        lastIndex = token.TokenIndex;

                        text = text.Substring(1, text.Length - 2); // Remove outer quotes.
                        var position = 0;
                        while (true)
                        {
                            position = text.IndexOf(doubledQuoteChar, position, StringComparison.Ordinal);
                            if (position == -1)
                                break;
                            text = text.Remove(position, 2).Insert(position, quoteChar.ToString());
                            position += 1;
                        }

                        result += text;
                        break;
                    }
                }
            }

            if (convertEscapes)
            {
                var temp = result;
                result = string.Empty;

                var pendingEscape = false;
                foreach (var ch in temp)
                {
                    var c = ch;
                    if (pendingEscape)
                    {
                        pendingEscape = false;
                        switch (c)
                        {
                            case 'n':
                                c = '\n';
                                break;
                            case 't':
                                c = '\t';
                                break;
                            case 'r':
                                c = '\r';
                                break;
                            case 'b':
                                c = '\b';
                                break;
                            case '0':
                                c = '\0';
                                break; // ASCII null
                            case 'Z':
                                c = '\x1A';
                                break; // Win32 end of file
                        }
                    }
                    else if (c == '\\')
                    {
                        pendingEscape = true;
                        continue;
                    }

                    result += c;
                }

                if (pendingEscape)
                    result += '\\';
            }

            return result;
        }

        return context.GetText();
    }

    protected bool Look(int position, int expected)
    {
        return InputStream.LA(position) == expected;
    }

    protected bool ContainsLinebreak(string text)
    {
        return text.IndexOfAny(new[] { '\r', '\n' }) != -1;
    }
}