using System;

namespace WDE.SmartScriptEditor.Validation.Antlr.Visitors
{
    class BinaryOperatorVisitor : SmartScriptValidationBaseVisitor<System.Func<long, long, long>>
    {
        public override Func<long, long, long> VisitBitwiseAnd(SmartScriptValidationParser.BitwiseAndContext context)
        {
            return (a, b) => a & b;
        }

        public override Func<long, long, long> VisitBitwiseOr(SmartScriptValidationParser.BitwiseOrContext context)
        {
            return (a, b) => a | b;
        }

        public override Func<long, long, long> VisitBitwiseXor(SmartScriptValidationParser.BitwiseXorContext context)
        {
            return (a, b) => a ^ b;
        }

        public override Func<long, long, long> VisitPlus(SmartScriptValidationParser.PlusContext context)
        {
            return (a, b) => a + b;
        }

        public override Func<long, long, long> VisitMinus(SmartScriptValidationParser.MinusContext context)
        {
            return (a, b) => a - b;
        }

        public override Func<long, long, long> VisitMultiply(SmartScriptValidationParser.MultiplyContext context)
        {
            return (a, b) => a * b;
        }

        public override Func<long, long, long> VisitDivide(SmartScriptValidationParser.DivideContext context)
        {
            return (a, b) => b == 0 ? int.MinValue : a / b;
        }

        public override Func<long, long, long> VisitModulo(SmartScriptValidationParser.ModuloContext context)
        {
            return (a, b) => b == 0 ? int.MinValue : a % b;
        }
    }
}