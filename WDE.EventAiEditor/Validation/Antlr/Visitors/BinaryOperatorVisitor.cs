using System;

namespace WDE.EventAiEditor.Validation.Antlr.Visitors
{
    class BinaryOperatorVisitor : EventAiValidationBaseVisitor<System.Func<long, long, long>>
    {
        public override Func<long, long, long> VisitBitwiseAnd(EventAiValidationParser.BitwiseAndContext context)
        {
            return (a, b) => a & b;
        }

        public override Func<long, long, long> VisitBitwiseOr(EventAiValidationParser.BitwiseOrContext context)
        {
            return (a, b) => a | b;
        }

        public override Func<long, long, long> VisitBitwiseXor(EventAiValidationParser.BitwiseXorContext context)
        {
            return (a, b) => a ^ b;
        }

        public override Func<long, long, long> VisitPlus(EventAiValidationParser.PlusContext context)
        {
            return (a, b) => a + b;
        }

        public override Func<long, long, long> VisitMinus(EventAiValidationParser.MinusContext context)
        {
            return (a, b) => a - b;
        }

        public override Func<long, long, long> VisitMultiply(EventAiValidationParser.MultiplyContext context)
        {
            return (a, b) => a * b;
        }

        public override Func<long, long, long> VisitDivide(EventAiValidationParser.DivideContext context)
        {
            return (a, b) => b == 0 ? int.MinValue : a / b;
        }

        public override Func<long, long, long> VisitModulo(EventAiValidationParser.ModuloContext context)
        {
            return (a, b) => b == 0 ? int.MinValue : a % b;
        }
    }
}