using Antlr4.Runtime.Tree;

namespace WDE.SmartScriptEditor.Validation.Antlr.Visitors
{
    class IntExpressionVisitor : SmartScriptValidationBaseVisitor<long>
    {
        private readonly ISmartValidationContext smartContext;
        private readonly BinaryOperatorVisitor binaryOperatorVisitor;

        public IntExpressionVisitor(ISmartValidationContext context)
        {
            smartContext = context;
            binaryOperatorVisitor = new BinaryOperatorVisitor();
        }
            
        public override long VisitENegate(SmartScriptValidationParser.ENegateContext context)
        {
            return -Visit(context.exprInt());
        }

        public override long VisitEMulOp(SmartScriptValidationParser.EMulOpContext context)
        {
            var left = Visit(context.exprInt()[0]);
            var right = Visit(context.exprInt()[1]);
            return binaryOperatorVisitor.Visit(context.mulOp())(left, right);
        }

        public override long VisitEParen(SmartScriptValidationParser.EParenContext context)
        {
            return Visit(context.exprInt());
        }

        public override long VisitEActionParam(SmartScriptValidationParser.EActionParamContext context)
        {
            if (!int.TryParse(context.INT().GetText(), out var asInt))
                throw new ValidationParseException("Integer out of range: " + context.INT().GetText());

            if (!smartContext.HasAction)
                throw new ValidationParseException("Trying to get Action parameter in context without action");
                
            if (asInt < 1 || asInt > smartContext.ActionParametersCount)
                throw new ValidationParseException("Action parameter out of bounds (" + asInt + ")");

            return smartContext.GetActionParameter(asInt - 1);
        }

        public override long VisitETargetParam(SmartScriptValidationParser.ETargetParamContext context)
        {
            if (!int.TryParse(context.INT().GetText(), out var asInt))
                throw new ValidationParseException("Integer out of range: " + context.INT().GetText());

            if (!smartContext.HasAction)
                throw new ValidationParseException("Trying to get Target parameter in context without action");
                
            if (asInt < 1 || asInt > smartContext.ActionTargetParametersCount)
                throw new ValidationParseException("Target parameter out of bounds (" + asInt + ")");

            return smartContext.GetActionTargetParameter(asInt - 1);
        }

        public override long VisitEInt(SmartScriptValidationParser.EIntContext context)
        {
            if (!int.TryParse(context.INT().GetText(), out var asInt))
                throw new ValidationParseException("Integer out of range: " + context.INT().GetText());
            return asInt;
        }

        public override long VisitESourceType(SmartScriptValidationParser.ESourceTypeContext context)
        {
            return (int) smartContext.ScriptType;
        }

        public override long VisitEAddOp(SmartScriptValidationParser.EAddOpContext context)
        {
            var left = Visit(context.exprInt()[0]);
            var right = Visit(context.exprInt()[1]);
            return binaryOperatorVisitor.Visit(context.addOp())(left, right);
        }

        public override long VisitETargetType(SmartScriptValidationParser.ETargetTypeContext context)
        {
            return smartContext.GetTargetType();
        }
        
        public override long VisitESourceParam(SmartScriptValidationParser.ESourceParamContext context)
        {
            if (!int.TryParse(context.INT().GetText(), out var asInt))
                throw new ValidationParseException("Integer out of range: " + context.INT().GetText());

            if (!smartContext.HasAction)
                throw new ValidationParseException("Trying to get Source parameter in context without action");
                
            if (asInt < 1 || asInt > smartContext.ActionSourceParametersCount)
                throw new ValidationParseException("Source parameter out of bounds (" + asInt + ")");

            return smartContext.GetActionSourceParameter(asInt - 1);
        }

        public override long VisitEEventParam(SmartScriptValidationParser.EEventParamContext context)
        {
            if (!int.TryParse(context.INT().GetText(), out var asInt))
                throw new ValidationParseException("Integer out of range: " + context.INT().GetText());

            if (asInt < 1 || asInt > smartContext.EventParametersCount)
                throw new ValidationParseException("Event parameter out of bounds (" + asInt + ")");

            return smartContext.GetEventParameter(asInt - 1);
        }

        public override long VisitChildren(IRuleNode node)
        {
            throw new ValidationParseException($"Unexpected node: {node.GetText()}");
        }
    }
}