using System;
using System.Linq;
using WDE.Common.Database;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Filtering.Antlr
{
    public class ExpressionVisitor : SyntaxBaseVisitor<object>
    {
        private PacketViewModel? packet;
        private static IsPacketPlayerProcessor isPlayerProcessor = new();
        private IsPacketSpecificPlayerProcessor isPacketSpecificPlayerProcessor;

        public ExpressionVisitor(IsPacketSpecificPlayerProcessor isPacketSpecificPlayerProcessor)
        {
            this.isPacketSpecificPlayerProcessor = isPacketSpecificPlayerProcessor;
        }

        public void SetContext(PacketViewModel packet)
        {
            this.packet = packet;
        }

        public override object VisitENegate(SyntaxParser.ENegateContext context)
        {
            var o = Visit(context.expr());
            if (o is bool b)
                return !b;
            throw new Exception("Bool expected in " + context.expr().GetText());
        }

        public override object VisitEGreaterEquals(SyntaxParser.EGreaterEqualsContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a.HasValue ? a.Value >= b : fa >= fb;
        }

        public (long?, long, float, float) EvalNumbers(SyntaxParser.ExprContext[] expr)
        {
            var a = Visit(expr[0]);
            var b = Visit(expr[1]);

            var l1 = a as long?;
            var l2 = b as long?;
            
            var f1 = a as float?;
            var f2 = b as float?;

            if (l1 != null && l2 != null)
                return (l1, l2.Value, 0, 0);
            
            if (f1 != null && l2 != null)
                return (null, 0, f1.Value, l2.Value);
            
            if (f2 != null && l1 != null)
                return (null, 0, l1.Value, f2.Value);
            
            if (f1 != null && f2 != null)
                return (null, 0, f1.Value, f2.Value);
            
            throw new Exception("Number expected in " + expr[0].GetText() + " and " + expr[1].GetText());
        }
        
        public (string, string) EvalStrings(SyntaxParser.ExprContext[] expr)
        {
            var a = Visit(expr[0]);
            var b = Visit(expr[1]);
            if (a is not string l1)
                throw new Exception("String expected in " + expr[0].GetText());
            if (b is not string l2)
                throw new Exception("String expected in " + expr[1].GetText());
            return (l1, l2);
        }
        
        public (bool, bool) EvalBools(SyntaxParser.ExprContext[] expr)
        {
            var a = Visit(expr[0]);
            var b = Visit(expr[1]);
            if (a is not bool l1)
                throw new Exception("Bool expected in " + expr[0].GetText());
            if (b is not bool l2)
                throw new Exception("Bool expected in " + expr[1].GetText());
            return (l1, l2);
        }

        public override object VisitEEquals(SyntaxParser.EEqualsContext context)
        {
            var a = Visit(context.expr()[0]);
            var b = Visit(context.expr()[1]);
            if (a is long l1 && b is long l2)
                return l1 == l2;
            if (a is bool b1 && b is bool b2)
                return b1 == b2;
            if (a is string s1 && b is string s2)
                return s1 == s2;
            throw new Exception($"You cannot compare {a.GetType()} and {b.GetType()}");
        }

        public override object VisitELessThan(SyntaxParser.ELessThanContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a.HasValue ? a.Value < b : fa < fb;
        }

        public override object VisitETrue(SyntaxParser.ETrueContext context)
        {
            return true;
        }

        public override object VisitEOr(SyntaxParser.EOrContext context)
        {
            var (a, b) = EvalBools(context.expr());
            return a || b;
        }

        public override object VisitEInt(SyntaxParser.EIntContext context)
        {
            if (!long.TryParse(context.INT().GetText(), out var asLong))
                throw new Exception(context.INT().GetText() + " expected to be a number");
            return asLong;
        }

        public override object VisitEFieldValue(SyntaxParser.EFieldValueContext context)
        {
            if (this.packet == null)
                return 0;
            var left = context.ID()[0].GetText();
            var right = context.ID()[1].GetText();
            if (left == "packet")
            {
                switch (right)
                {
                    case "opcode":
                        return packet.Opcode;
                    case "text":
                        return packet.Text;
                    case "id":
                        return (long) packet.Id;
                    case "entry":
                        return (long) packet.Entry;
                }
            }
            else
                return left + "_" + right;
            

            throw new Exception($"Unknown field {left}.{right}");
        }

        public override object VisitEIn(SyntaxParser.EInContext context)
        {
            var (a, b) = EvalStrings(context.expr());
            return b.Contains(a);
        }

        public override object VisitELessEquals(SyntaxParser.ELessEqualsContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a.HasValue ? a.Value <= b : fa <= fb;
        }

        public override object VisitEAnd(SyntaxParser.EAndContext context)
        {
            var (a, b) = EvalBools(context.expr());
            return a && b;
        }

        public override object VisitEMulOp(SyntaxParser.EMulOpContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a * b ?? fa * fb;
        }

        public override object VisitEDivOp(SyntaxParser.EDivOpContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a / b ?? fa / fb;
        }

        public override object VisitEPlusOp(SyntaxParser.EPlusOpContext context)
        {
            var va = Visit(context.expr()[0]);
            var vb = Visit(context.expr()[1]);
            if (va is string s1 && vb is string s2)
                return s1 + s2;
            
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a + b ?? fa + fb;
        }

        public override object VisitEParen(SyntaxParser.EParenContext context)
        {
            return Visit(context.expr());
        }

        public override object VisitEFalse(SyntaxParser.EFalseContext context)
        {
            return false;
        }

        public override object VisitEGreaterThan(SyntaxParser.EGreaterThanContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a.HasValue ? a > b : fa > fb;
        }

        public override object VisitENotEquals(SyntaxParser.ENotEqualsContext context)
        {
            var a = Visit(context.expr()[0]);
            var b = Visit(context.expr()[1]);
            if (a is long l1 && b is long l2)
                return l1 != l2;
            if (a is bool b1 && b is bool b2)
                return b1 != b2;
            if (a is string s1 && b is string s2)
                return s1 != s2;
            throw new Exception("You cannot compare not matching types");
        }

        public override object VisitEFuncAppl(SyntaxParser.EFuncApplContext context)
        {
            var args = context.expr().Select(Visit).ToList();
            var functionName = context.ID().GetText();
            if (functionName == "str")
            {
                if (args[0] is float f)
                    return f.ToString("0.0#");
                return args[0].ToString()!;
            }
            if (functionName == "int")
            {
                if (args[0] is long l)
                    return l;
                else if (args[0] is float f)
                    return (long)f;
                else if (args[0] is string s && long.TryParse(s, out var num))
                    return num;
                return 0L;
            }
            if (functionName == "lower")
            {
                if (args[0] is string l)
                    return l.ToLower();
                throw new Exception("First argument of lower() must be a string!");
            }
            if (functionName == "ceil")
            {
                if (args[0] is long l)
                    return l;
                else if (args[0] is float f)
                    return (long)Math.Ceiling(f);
                return 0L;
            }
            if (functionName == "is_me")
                return isPacketSpecificPlayerProcessor.Process(packet!.Packet);
            if (functionName == "is_player")
                return isPlayerProcessor.Process(packet!.Packet);
            throw new Exception($"Unknown function {functionName}");
        }

        public override object VisitEStr(SyntaxParser.EStrContext context)
        {
            var text = context.STRING().GetText() ?? "''";
            return text.Substring(1, text.Length - 2);
        }
    }
}