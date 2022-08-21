using Antlr4.Runtime.Tree;

namespace WDE.EventAiEditor.Validation.Antlr.Visitors
{
    class BoolExpressionVisitor : EventAiValidationBaseVisitor<bool>
    {
        private readonly IntExpressionVisitor intVisitor;

        public BoolExpressionVisitor(IntExpressionVisitor intExpressionVisitor)
        {
            intVisitor = intExpressionVisitor;
        }
            
        public override bool VisitBEqualsInt(EventAiValidationParser.BEqualsIntContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) == intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBGreaterThan(EventAiValidationParser.BGreaterThanContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) > intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBOr(EventAiValidationParser.BOrContext context)
        {
            return Visit(context.exprBool()[0]) || Visit(context.exprBool()[1]);
        }

        public override bool VisitBNotEqualsInt(EventAiValidationParser.BNotEqualsIntContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) != intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBFalse(EventAiValidationParser.BFalseContext context)
        {
            return false;
        }

        public override bool VisitBEquals(EventAiValidationParser.BEqualsContext context)
        {
            return Visit(context.exprBool()[0]) == Visit(context.exprBool()[1]);
        }

        public override bool VisitBAnd(EventAiValidationParser.BAndContext context)
        {
            return Visit(context.exprBool()[0]) && Visit(context.exprBool()[1]);
        }

        public override bool VisitBGreaterEquals(EventAiValidationParser.BGreaterEqualsContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) >= intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBNegate(EventAiValidationParser.BNegateContext context)
        {
            return !Visit(context.exprBool());
        }

        public override bool VisitBLessThan(EventAiValidationParser.BLessThanContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) < intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBTrue(EventAiValidationParser.BTrueContext context)
        {
            return true;
        }

        public override bool VisitBNotEquals(EventAiValidationParser.BNotEqualsContext context)
        {
            return Visit(context.exprBool()[0]) != Visit(context.exprBool()[1]);
        }

        public override bool VisitBParen(EventAiValidationParser.BParenContext context)
        {
            return Visit(context.exprBool());
        }

        public override bool VisitBLessEquals(EventAiValidationParser.BLessEqualsContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) <= intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitChildren(IRuleNode node)
        {
            throw new ValidationParseException($"Unexpected node: {node.GetText()}");
        }
    }
}