using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using AvaloniaEdit.Document;
using Microsoft.Extensions.ObjectPool;
using WDE.SqlWorkbench.Antlr;

namespace WDE.SqlWorkbench.Services.SyntaxValidator;

internal class AntlrSyntaxValidator : ISyntaxValidator
{
    private ObjectPool<AntlrClassesSet> sets = new DefaultObjectPool<AntlrClassesSet>(new DefaultPooledObjectPolicy<AntlrClassesSet>());

    private class AntlrClassesSet : IResettable
    {
        private AntlrErrorListener errorListener = new AntlrErrorListener();
        private NoCopyCharStream inputStream;
        private MySQLLexer lexer;
        private CommonTokenStream tokens;
        private MySQLParser parser;
        private MySQLParserBaseListener listener;

        public AntlrClassesSet()
        {
            errorListener = new AntlrErrorListener();
            inputStream = new NoCopyCharStream(null!, 0, 0);
            lexer = new MySQLLexer(inputStream); // CaseChangingCharStream
            lexer.RemoveErrorListeners();
            tokens = new CommonTokenStream(lexer);
            parser = new MySQLParser(tokens);
            listener = new MySQLParserBaseListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
        }
        
        public void Walk(List<SyntaxError> output, ITextSource source, int start, int length)
        {
            errorListener.SetOutput(output);
            inputStream.Reset(source, start, length);
            lexer.SetInputStream(inputStream);
            tokens.SetTokenSource(lexer);
            parser.TokenStream = tokens;
            parser.Reset();
            ParseTreeWalker walker = new ParseTreeWalker();
            walker.Walk(new MySQLParserBaseListener(), parser.query());
        }
        
        public bool TryReset()
        {
            inputStream.Reset(null!, 0, 0);
            errorListener.SetOutput(null!);
            return true;
        }
    }
    
    public class AntlrErrorListener : IAntlrErrorListener<IToken>
    {
        private List<SyntaxError> output = null!;

        public AntlrErrorListener()
        {
        }
        
        public void SetOutput(List<SyntaxError> output)
        {
            this.output = output;
        }
        
        private static Dictionary<string, string> friendlyDescription = new()
        {
            { "BACK_TICK_QUOTED_ID", "`text`" },
            { "DOUBLE_QUOTED_TEXT", "\"text\"" },
            { "SINGLE_QUOTED_TEXT", "'text'" },
        };

        private string IntervalToString(IntervalSet set, int maxCount, Vocabulary vocabulary)
        {
            List<int> symbols = set.ToIntegerList();

            if (symbols.Count == 0)
            {
                return "";
            }

            StringBuilder ss = new StringBuilder();
            bool firstEntry = true;
            maxCount = Math.Min(maxCount, symbols.Count);
            for (int i = 0; i < maxCount; ++i)
            {
                int symbol = symbols[i];
                if (!firstEntry)
                    ss.Append(", ");
                firstEntry = false;

                if (symbol < 0)
                {
                    ss.Append("EOF");
                }
                else
                {
                    string name = vocabulary.GetDisplayName(symbol);
                    if (name.Contains("_SYMBOL"))
                        name = name.Substring(0, name.Length - 7);
                    else if (name.Contains("_OPERATOR"))
                        name = name.Substring(0, name.Length - 9);
                    else if (name.Contains("_NUMBER"))
                        name = name.Substring(0, name.Length - 7) + " number";
                    else
                    {
                        if (friendlyDescription.TryGetValue(name, out var fd))
                            name = fd;
                    }

                    ss.Append(name);
                }
            }

            if (maxCount < symbols.Count)
                ss.Append(", ...");

            return ss.ToString();
        }

        private static HashSet<int> simpleRules = new()
        {
            MySQLParser.RULE_identifier,
            MySQLParser.RULE_qualifiedIdentifier,
        };

        private static Dictionary<int, string> objectNames = new()
        {
            { MySQLParser.RULE_columnName, "column" },
            { MySQLParser.RULE_columnRef, "column" },
            { MySQLParser.RULE_columnInternalRef, "column" },
            { MySQLParser.RULE_indexName, "index" },
            { MySQLParser.RULE_indexRef, "index" },
            { MySQLParser.RULE_schemaName, "schema" },
            { MySQLParser.RULE_schemaRef, "schema" },
            { MySQLParser.RULE_procedureName, "procedure" },
            { MySQLParser.RULE_procedureRef, "procedure" },
            { MySQLParser.RULE_functionName, "function" },
            { MySQLParser.RULE_functionRef, "function" },
            { MySQLParser.RULE_triggerName, "trigger" },
            { MySQLParser.RULE_triggerRef, "trigger" },
            { MySQLParser.RULE_viewName, "view" },
            { MySQLParser.RULE_viewRef, "view" },
            { MySQLParser.RULE_tablespaceName, "tablespace" },
            { MySQLParser.RULE_tablespaceRef, "tablespace" },
            { MySQLParser.RULE_logfileGroupName, "logfile group" },
            { MySQLParser.RULE_logfileGroupRef, "logfile group" },
            { MySQLParser.RULE_eventName, "event" },
            { MySQLParser.RULE_eventRef, "event" },
            { MySQLParser.RULE_udfName, "udf" },
            { MySQLParser.RULE_serverName, "server" },
            { MySQLParser.RULE_serverRef, "server" },
            { MySQLParser.RULE_engineRef, "engine" },
            { MySQLParser.RULE_tableName, "table" },
            { MySQLParser.RULE_tableRef, "table" },
            { MySQLParser.RULE_filterTableRef, "table" },
            { MySQLParser.RULE_tableRefWithWildcard, "table" },
            { MySQLParser.RULE_parameterName, "parameter" },
            { MySQLParser.RULE_labelIdentifier, "label" },
            { MySQLParser.RULE_labelRef, "label" },
            { MySQLParser.RULE_roleIdentifier, "role" },
            { MySQLParser.RULE_roleRef, "role" },
            { MySQLParser.RULE_pluginRef, "plugin" },
            { MySQLParser.RULE_componentRef, "component" },
            { MySQLParser.RULE_resourceGroupRef, "resource group" },
            { MySQLParser.RULE_windowName, "window" },
        };

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException ep)
        {
            string message = "";
            MySQLParser parser = (recognizer as MySQLParser)!;
            MySQLLexer lexer = (MySQLLexer)parser.TokenStream.TokenSource;
            bool isEof = offendingSymbol.Type == MySQLLexer.Eof;
            if (isEof)
            {
                // In order to show a good error marker look at the previous token if we are at the input end.
                IToken previous = parser.TokenStream.LT(-1);//parser.TokenStream.Size - 1);
                if (previous != null)
                    offendingSymbol = previous;
            }
            

            string wrongText = offendingSymbol.Text;

            // getExpectedTokens() ignores predicates, so it might include the token for which we got this syntax error,
            // if that was excluded by a predicate (which in our case is always a version check).
            // That's a good indicator to tell the user that this keyword is not valid *for the current server version*.
            IntervalSet expected = parser.GetExpectedTokens();
            bool invalidForVersion = false;
            int tokenType = offendingSymbol.Type;
            if (tokenType != MySQLLexer.IDENTIFIER && expected.Contains(tokenType))
                invalidForVersion = true;
            else
            {
                tokenType = lexer.KeywordFromText(wrongText);
                if (expected.Contains(tokenType))
                    invalidForVersion = true;
            }

            if (invalidForVersion)
            {
                if (expected.IsReadOnly)
                    expected = new IntervalSet(expected);
                expected.Remove(tokenType);
            }

            // Try to find the expected input by examining the current parser context and
            // the expected interval set. The latter often lists useless keywords, especially if they are allowed
            // as identifiers.
            string expectedText;


            // Walk up from generic rules to reach something that gives us more context, if needed.
            ParserRuleContext context = parser.RuleContext;
            while (simpleRules.Contains(context.RuleIndex))
                context = (ParserRuleContext)context.Parent;

            switch (context.RuleIndex)
            {
                case MySQLParser.RULE_functionCall:
                    expectedText = "a complete function call or other expression";
                    break;

                case MySQLParser.RULE_expr:
                    expectedText = "an expression";
                    break;

                case MySQLParser.RULE_columnName:
                case MySQLParser.RULE_indexName:
                case MySQLParser.RULE_schemaName:
                case MySQLParser.RULE_procedureName:
                case MySQLParser.RULE_functionName:
                case MySQLParser.RULE_triggerName:
                case MySQLParser.RULE_viewName:
                case MySQLParser.RULE_tablespaceName:
                case MySQLParser.RULE_logfileGroupName:
                case MySQLParser.RULE_eventName:
                case MySQLParser.RULE_udfName:
                case MySQLParser.RULE_serverName:
                case MySQLParser.RULE_tableName:
                case MySQLParser.RULE_parameterName:
                case MySQLParser.RULE_labelIdentifier:
                case MySQLParser.RULE_roleIdentifier:
                case MySQLParser.RULE_windowName:
                {
                    if (!objectNames.TryGetValue(context.RuleIndex, out var objectName))
                        expectedText = "a new object name";
                    else
                        expectedText = "a new " + objectName + " name";
                    break;
                }

                case MySQLParser.RULE_columnRef:
                case MySQLParser.RULE_indexRef:
                case MySQLParser.RULE_schemaRef:
                case MySQLParser.RULE_procedureRef:
                case MySQLParser.RULE_functionRef:
                case MySQLParser.RULE_triggerRef:
                case MySQLParser.RULE_viewRef:
                case MySQLParser.RULE_tablespaceRef:
                case MySQLParser.RULE_logfileGroupRef:
                case MySQLParser.RULE_eventRef:
                case MySQLParser.RULE_serverRef:
                case MySQLParser.RULE_engineRef:
                case MySQLParser.RULE_tableRef:
                case MySQLParser.RULE_filterTableRef:
                case MySQLParser.RULE_tableRefWithWildcard:
                case MySQLParser.RULE_labelRef:
                case MySQLParser.RULE_roleRef:
                case MySQLParser.RULE_pluginRef:
                case MySQLParser.RULE_componentRef:
                case MySQLParser.RULE_resourceGroupRef:
                {
                    if (!objectNames.TryGetValue(context.RuleIndex, out var objectName))
                        expectedText = "the name of an existing object";
                    else
                        expectedText = "the name of an existing " + objectName;
                    break;
                }

                case MySQLParser.RULE_columnInternalRef:
                    expectedText = "a column name from this table";
                    break;

                default:
                {
                    // If the expected set contains the IDENTIFIER token we likely want an identifier at this position.
                    // Due to the fact that MySQL defines a number of keywords as possible identifiers, we get all those
                    // whenever an identifier is actually required, bloating so the expected set with irrelevant elements.
                    // Hence we check for the identifier entry and assume we *only* want an identifier. This gives an unprecise
                    // result if both certain keywords *and* an identifier are expected.
                    if (expected.Contains(MySQLLexer.IDENTIFIER))
                        expectedText = "an identifier";
                    else
                        expectedText = IntervalToString(expected, 6, (Vocabulary)parser.Vocabulary);
                    break;
                }
            }

            if (wrongText[0] != '"' && wrongText[0] != '\'' && wrongText[0] != '`')
                wrongText = "\"" + wrongText + "\"";

            if (ep == null)
            {
                // Missing or unwanted tokens.
                if (msg.Contains("missing"))
                {
                    if (expected.Count == 1)
                    {
                        message = "Missing " + expectedText;
                    }
                }
                else
                {
                    message = "Extraneous input " + wrongText + " found, expecting " + expectedText;
                }
            }
            else
            {
                try
                {
                    throw ep;
                }
                catch (InputMismatchException)
                {
                    if (isEof)
                        message = "Statement is incomplete";
                    else
                        message = wrongText + " is not valid at this position";

                    if (expectedText.Length != 0)
                        message += ", expecting " + expectedText;
                }
                catch (FailedPredicateException e)
                {
                    // For cases like "... | a ({condition}? b)", but not "... | a ({condition}? b)?".
                    string condition = e.Message;
                    //string prefix = "predicate failed: ";
                    //condition.erase(0, prefix.size());
                    //condition.resize(condition.size() - 1); // Remove trailing question mark.

                    condition = condition.Replace("serverVersion", "server version");
                    condition = condition.Replace("&&", "and");
                    message = wrongText + " is valid only for " + condition;
                }
                catch (NoViableAltException)
                {
                    if (isEof)
                        message = "Statement is incomplete";
                    else
                    {
                        message = wrongText + " is not valid at this position";
                        if (invalidForVersion)
                            message += " for this server version";
                    }

                    if (expectedText.Length > 0)
                        message += ", expecting " + expectedText;
                }
            }
            
            this.output.Add(new SyntaxError(offendingSymbol.Line, offendingSymbol.StartIndex, offendingSymbol.StopIndex - offendingSymbol.StartIndex + 1, message));
        }
    }
    
    public async Task ValidateAsync(ITextSource source, int start, int length, List<SyntaxError> output, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() =>
            {
                AntlrClassesSet? set = null;
                try
                {
                    set = sets.Get();
                    set.Walk(output, source, start, length);
                }
                finally
                {
                    if (set != null)
                        sets.Return(set);
                }
            }, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            
        }
    }
}