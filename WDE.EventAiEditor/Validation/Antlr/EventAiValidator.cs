using System;
using System.IO;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using WDE.Module.Attributes;
using WDE.EventAiEditor.Validation.Antlr.Visitors;

[assembly: InternalsVisibleTo("WDE.EventAiEditor.Test")]
namespace WDE.EventAiEditor.Validation.Antlr
{
    [AutoRegister]
    [SingleInstance]
    public class EventAiValidator : IEventAiValidator
    {
        private EventAiValidationParser parser;
        private EventAiValidationParser.ExprBoolContext? boolContextCached;
        private EventAiValidationParser.ExprIntContext? intContextCached;
        
        public EventAiValidator(string expression)
        {
            var lexer = new EventAiValidationLexer(new AntlrInputStream(expression));
            var tokens = new CommonTokenStream(lexer);
            parser = new EventAiValidationParser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new CustomErrorListener());
        }
        
        private T WrapWithTryCatch<T>(Func<EventAiValidationParser, T> action)
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
        
        public bool Evaluate(IEventAiValidationContext context)
        {
            return WrapWithTryCatch(parser =>
            {
                BoolExpressionVisitor visitor = new(new IntExpressionVisitor(context));
                boolContextCached ??= parser.exprBool();
                return visitor.Visit(boolContextCached);
            });
        }
        
        internal long EvaluateInteger(IEventAiValidationContext context)
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