using System;
using System.IO;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Validation.Antlr.Visitors;

[assembly: InternalsVisibleTo("WDE.SmartScriptEditor.Test")]
namespace WDE.SmartScriptEditor.Validation.Antlr
{
    [AutoRegister]
    [SingleInstance]
    public class SmartValidator : ISmartValidator
    {
        private SmartScriptValidationParser parser;
        private SmartScriptValidationParser.ExprBoolContext? boolContextCached;
        private SmartScriptValidationParser.ExprIntContext? intContextCached;
        
        public SmartValidator(string expression)
        {
            var lexer = new SmartScriptValidationLexer(new AntlrInputStream(expression));
            var tokens = new CommonTokenStream(lexer);
            parser = new SmartScriptValidationParser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new CustomErrorListener());
        }
        
        private T WrapWithTryCatch<T>(Func<SmartScriptValidationParser, T> action)
        {
            try
            {
                return action(parser);
            }
            catch (ValidationParseException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ValidationParseException("Error while parsing " + e.Message);
            }
        }
        
        public bool Evaluate(ISmartValidationContext context)
        {
            return WrapWithTryCatch(parser =>
            {
                BoolExpressionVisitor visitor = new(context, new IntExpressionVisitor(context));
                boolContextCached ??= parser.exprBool();
                return visitor.Visit(boolContextCached);
            });
        }
        
        internal long EvaluateInteger(ISmartValidationContext context)
        {
            return WrapWithTryCatch(parser =>
            {
                var visitor = new IntExpressionVisitor(context);
                intContextCached ??= parser.exprInt();
                return visitor.Visit(intContextCached);
            });
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
                throw new ValidationParseException($"Parse error in line {line} at position {charPositionInLine}: {msg}");
            }
        }
    }
}