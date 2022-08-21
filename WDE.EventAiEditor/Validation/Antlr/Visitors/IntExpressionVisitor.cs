using Antlr4.Runtime.Tree;

namespace WDE.EventAiEditor.Validation.Antlr.Visitors
{
    class IntExpressionVisitor : EventAiValidationBaseVisitor<long>
    {
        private readonly IEventAiValidationContext eventAiContext;
        private readonly BinaryOperatorVisitor binaryOperatorVisitor;

        public IntExpressionVisitor(IEventAiValidationContext context)
        {
            eventAiContext = context;
            binaryOperatorVisitor = new BinaryOperatorVisitor();
        }
            
        public override long VisitENegate(EventAiValidationParser.ENegateContext context)
        {
            return -Visit(context.exprInt());
        }

        public override long VisitEMulOp(EventAiValidationParser.EMulOpContext context)
        {
            var left = Visit(context.exprInt()[0]);
            var right = Visit(context.exprInt()[1]);
            return binaryOperatorVisitor.Visit(context.mulOp())(left, right);
        }

        public override long VisitEParen(EventAiValidationParser.EParenContext context)
        {
            return Visit(context.exprInt());
        }

        public override long VisitEActionParam(EventAiValidationParser.EActionParamContext context)
        {
            if (!int.TryParse(context.INT().GetText(), out var asInt))
                throw new ValidationParseException("Integer out of range: " + context.INT().GetText());

            if (!eventAiContext.HasAction)
                throw new ValidationParseException("Trying to get Action parameter in context without action");
                
            if (asInt < 1 || asInt > eventAiContext.ActionParametersCount)
                throw new ValidationParseException("Action parameter out of bounds (" + asInt + ")");

            return eventAiContext.GetActionParameter(asInt - 1);
        }

        public override long VisitEInt(EventAiValidationParser.EIntContext context)
        {
            if (!int.TryParse(context.INT().GetText(), out var asInt))
                throw new ValidationParseException("Integer out of range: " + context.INT().GetText());
            return asInt;
        }

        public override long VisitEAddOp(EventAiValidationParser.EAddOpContext context)
        {
            var left = Visit(context.exprInt()[0]);
            var right = Visit(context.exprInt()[1]);
            return binaryOperatorVisitor.Visit(context.addOp())(left, right);
        }

        public override long VisitEEventParam(EventAiValidationParser.EEventParamContext context)
        {
            if (!int.TryParse(context.INT().GetText(), out var asInt))
                throw new ValidationParseException("Integer out of range: " + context.INT().GetText());

            if (asInt < 1 || asInt > eventAiContext.EventParametersCount)
                throw new ValidationParseException("Event parameter out of bounds (" + asInt + ")");

            return eventAiContext.GetEventParameter(asInt - 1);
        }

        public override long VisitEEventFlags(EventAiValidationParser.EEventFlagsContext context)
        {
            return eventAiContext.GetEventFlags();
        }

        public override long VisitChildren(IRuleNode node)
        {
            throw new ValidationParseException($"Unexpected node: {node.GetText()}");
        }
    }
}