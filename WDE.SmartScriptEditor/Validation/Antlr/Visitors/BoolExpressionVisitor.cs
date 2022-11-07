using System;
using Antlr4.Runtime.Tree;

namespace WDE.SmartScriptEditor.Validation.Antlr.Visitors
{
    class BoolExpressionVisitor : SmartScriptValidationBaseVisitor<bool>
    {
        private readonly IntExpressionVisitor intVisitor;

        private readonly ISmartValidationContext smartContext;

        public BoolExpressionVisitor(ISmartValidationContext context, IntExpressionVisitor intExpressionVisitor)
        {
            smartContext = context;
            intVisitor = intExpressionVisitor;
        }
            
        public override bool VisitBEqualsInt(SmartScriptValidationParser.BEqualsIntContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) == intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBGreaterThan(SmartScriptValidationParser.BGreaterThanContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) > intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBOr(SmartScriptValidationParser.BOrContext context)
        {
            return Visit(context.exprBool()[0]) || Visit(context.exprBool()[1]);
        }

        public override bool VisitBNotEqualsInt(SmartScriptValidationParser.BNotEqualsIntContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) != intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBFalse(SmartScriptValidationParser.BFalseContext context)
        {
            return false;
        }

        public override bool VisitBEquals(SmartScriptValidationParser.BEqualsContext context)
        {
            return Visit(context.exprBool()[0]) == Visit(context.exprBool()[1]);
        }

        public override bool VisitBAnd(SmartScriptValidationParser.BAndContext context)
        {
            return Visit(context.exprBool()[0]) && Visit(context.exprBool()[1]);
        }

        public override bool VisitBGreaterEquals(SmartScriptValidationParser.BGreaterEqualsContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) >= intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBNegate(SmartScriptValidationParser.BNegateContext context)
        {
            return !Visit(context.exprBool());
        }

        public override bool VisitBLessThan(SmartScriptValidationParser.BLessThanContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) < intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBTrue(SmartScriptValidationParser.BTrueContext context)
        {
            return true;
        }

        public override bool VisitBNotEquals(SmartScriptValidationParser.BNotEqualsContext context)
        {
            return Visit(context.exprBool()[0]) != Visit(context.exprBool()[1]);
        }

        public override bool VisitBParen(SmartScriptValidationParser.BParenContext context)
        {
            return Visit(context.exprBool());
        }

        public override bool VisitBLessEquals(SmartScriptValidationParser.BLessEqualsContext context)
        {
            return intVisitor.Visit(context.exprInt()[0]) <= intVisitor.Visit(context.exprInt()[1]);
        }

        public override bool VisitBTargetPosEmpty(SmartScriptValidationParser.BTargetPosEmptyContext context)
        {
            var pos = smartContext.GetTargetPosition();
            return Math.Abs(pos.X) < float.Epsilon && Math.Abs(pos.Y) < float.Epsilon && Math.Abs(pos.Z) < float.Epsilon;
        }

        public override bool VisitChildren(IRuleNode node)
        {
            throw new ValidationParseException($"Unexpected node: {node.GetText()}");
        }
    }
}