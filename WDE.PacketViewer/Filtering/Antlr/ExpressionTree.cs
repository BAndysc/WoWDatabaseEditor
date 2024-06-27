using System;
using System.Collections.Generic;
using System.Linq;

namespace WDE.PacketViewer.Filtering.Antlr;

public class ExpressionTree
{
    public class TreeBuilder : SyntaxBaseVisitor<ITreeNode>
    {
        public override ITreeNode VisitENegate(SyntaxParser.ENegateContext context) => new ENegate(Visit(context.expr()), context.GetText());

        public override ITreeNode VisitEGreaterEquals(SyntaxParser.EGreaterEqualsContext context)
        {
            var exprs = context.expr();
            return new EGreaterEquals(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitEEquals(SyntaxParser.EEqualsContext context)
        {
            var exprs = context.expr();
            return new EEquals(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitELessThan(SyntaxParser.ELessThanContext context)
        {
            var exprs = context.expr();
            return new ELessThan(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitETrue(SyntaxParser.ETrueContext context)
        {
            return new ETrue();
        }

        public override ITreeNode VisitEOr(SyntaxParser.EOrContext context)
        {
            var exprs = context.expr();
            return new EOr(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitEInt(SyntaxParser.EIntContext context)
        {
            if (!long.TryParse(context.INT().GetText(), out var asLong))
                throw new Exception(context.INT().GetText() + " expected to be a number");
            return new EInt(asLong);
        }

        public override ITreeNode VisitEFieldValue(SyntaxParser.EFieldValueContext context)
        {
            var identifiers = context.ID();
            return new EFieldValue(identifiers.Select(i => i.GetText()).ToList(), context.GetText());
        }

        public override ITreeNode VisitEIn(SyntaxParser.EInContext context)
        {
            var exprs = context.expr();
            return new EIn(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitELessEquals(SyntaxParser.ELessEqualsContext context)
        {
            var exprs = context.expr();
            return new ELessEquals(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitEAnd(SyntaxParser.EAndContext context)
        {
            var exprs = context.expr();
            return new EAnd(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitEMulOp(SyntaxParser.EMulOpContext context)
        {
            var exprs = context.expr();
            return new EMulOp(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitEDivOp(SyntaxParser.EDivOpContext context)
        {
            var exprs = context.expr();
            return new EDivOp(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitEPlusOp(SyntaxParser.EPlusOpContext context)
        {
            var exprs = context.expr();
            return new EPlusOp(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitEParen(SyntaxParser.EParenContext context)
        {
            return new EParen(Visit(context.expr()), context.GetText());
        }

        public override ITreeNode VisitEFalse(SyntaxParser.EFalseContext context)
        {
            return new EFalse();
        }

        public override ITreeNode VisitEGreaterThan(SyntaxParser.EGreaterThanContext context)
        {
            var exprs = context.expr();
            return new EGreaterThan(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitENotEquals(SyntaxParser.ENotEqualsContext context)
        {
            var exprs = context.expr();
            return new ENotEquals(Visit(exprs[0]), Visit(exprs[1]), context.GetText());
        }

        public override ITreeNode VisitEFuncAppl(SyntaxParser.EFuncApplContext context)
        {
            var args = context.expr().Select(Visit).ToList();
            var functionName = context.ID().GetText();
            return new EFuncAppl(args, functionName, context.GetText());
        }

        public override ITreeNode VisitEStr(SyntaxParser.EStrContext context)
        {
            var text = context.STRING().GetText() ?? "''";
            text = text.Substring(1, text.Length - 2);
            return new EStr(text);
        }
    }

    public interface ITreeNode
    {
        string Text { get; }
        T Accept<T>(Visitor<T> visitor);
    }

    public class ENegate(ITreeNode expr, string text) : ITreeNode
    {
        public ITreeNode Expr { get; } = expr;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitENegate(this);
    }

    public class EGreaterEquals(ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEGreaterEquals(this);
    }

    public class EEquals(ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEEquals(this);
    }

    public class ELessThan(ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitELessThan(this);
    }

    public class ETrue : ITreeNode
    {
        public string Text => "true";
        public T Accept<T>(Visitor<T> visitor) => visitor.VisitETrue(this);
    }

    public class EOr(ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEOr(this);
    }

    public class EInt(long value) : ITreeNode
    {
        public long Value { get; } = value;

        public string Text => Value.ToString();

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEInt(this);
    }

    public class EFieldValue(IReadOnlyList<string> identifiers, string text) : ITreeNode
    {
        public IReadOnlyList<string> Identifiers { get; } = identifiers;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEFieldValue(this);
    }

    public class EIn (ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEIn(this);
    }

    public class ELessEquals (ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitELessEquals(this);
    }

    public class EAnd (ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEAnd(this);
    }

    public class EMulOp (ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEMulOp(this);
    }

    public class EDivOp (ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEDivOp(this);
    }

    public class EPlusOp (ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEPlusOp(this);
    }

    public class EParen(ITreeNode inner, string text) : ITreeNode
    {
        public ITreeNode Inner { get; } = inner;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEParen(this);
    }

    public class EFalse : ITreeNode
    {
        public string Text => "false";
        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEFalse(this);
    }

    public class EGreaterThan(ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEGreaterThan(this);
    }

    public class ENotEquals(ITreeNode a, ITreeNode b, string text) : ITreeNode
    {
        public ITreeNode A { get; } = a;
        public ITreeNode B { get; } = b;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitENotEquals(this);
    }

    public class EFuncAppl(List<ITreeNode> args, string functionName, string text) : ITreeNode
    {
        public List<ITreeNode> Args { get; } = args;
        public string FunctionName { get; } = functionName;
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEFuncAppl(this);
    }

    public class EStr(string text) : ITreeNode
    {
        public string Text { get; } = text;

        public T Accept<T>(Visitor<T> visitor) => visitor.VisitEStr(this);
    }

    public abstract class Visitor<T>
    {
        public abstract T VisitENegate(ENegate node);
        public abstract T VisitEGreaterEquals(EGreaterEquals node);
        public abstract T VisitEEquals(EEquals node);
        public abstract T VisitELessThan(ELessThan node);
        public abstract T VisitETrue(ETrue node);
        public abstract T VisitEOr(EOr node);
        public abstract T VisitEInt(EInt node);
        public abstract T VisitEFieldValue(EFieldValue node);
        public abstract T VisitEIn(EIn node);
        public abstract T VisitELessEquals(ELessEquals node);
        public abstract T VisitEAnd(EAnd node);
        public abstract T VisitEMulOp(EMulOp node);
        public abstract T VisitEDivOp(EDivOp node);
        public abstract T VisitEPlusOp(EPlusOp node);
        public abstract T VisitEParen(EParen node);
        public abstract T VisitEFalse(EFalse node);
        public abstract T VisitEGreaterThan(EGreaterThan node);
        public abstract T VisitENotEquals(ENotEquals node);
        public abstract T VisitEFuncAppl(EFuncAppl node);
        public abstract T VisitEStr(EStr node);

        public T Visit(ITreeNode node)
        {
            return node.Accept(this);
        }
    }
}