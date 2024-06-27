using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Antlr4.Runtime.Tree;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Filtering.Antlr
{
    public struct FilterOutput
    {
        private string? text;
        private PackedScalars scalars;
        private Kind kind;

        public bool IsString => kind == Kind.String;
        public bool IsNumber => kind == Kind.Number;
        public bool IsFloat => kind == Kind.Float;
        public bool IsBoolean => kind == Kind.Boolean;

        public long? Long => kind == Kind.Number ? scalars.Long : null;
        public float? Float => kind == Kind.Float ? scalars.Float : null;
        public bool? Boolean => kind == Kind.Boolean ? scalars.Boolean : null;
        public string? Text => kind == Kind.String ? text : null;

        public static FilterOutput FromString(string s)
        {
            return new FilterOutput()
            {
                text = s,
                kind = Kind.String
            };
        }

        public static FilterOutput FromNumber(long n)
        {
            return new FilterOutput()
            {
                scalars = n,
                kind = Kind.Number
            };
        }

        public static FilterOutput FromBoolean(bool b)
        {
            return new FilterOutput()
            {
                scalars = b,
                kind = Kind.Boolean
            };
        }

        public static FilterOutput FromFloat(float f)
        {
            return new FilterOutput()
            {
                scalars = f,
                kind = Kind.Float
            };
        }

        public static implicit operator FilterOutput(bool b) => FromBoolean(b);

        public static implicit operator FilterOutput(long n) => FromNumber(n);
        
        public static implicit operator FilterOutput(string s) => FromString(s);

        [StructLayout(LayoutKind.Explicit)]
        private struct PackedScalars
        {
            [FieldOffset(0)] public long Long;
            [FieldOffset(0)] public float Float;
            [FieldOffset(0)] public bool Boolean;

            public static implicit operator PackedScalars(bool b) => new PackedScalars() {Boolean = b};
            public static implicit operator PackedScalars(long l) => new PackedScalars() {Long = l};
            public static implicit operator PackedScalars(float f) => new PackedScalars() {Float = f};
        }

        private enum Kind
        {
            String,
            Number,
            Float,
            Boolean
        }

        public override string ToString()
        {
            if (IsString)
                return Text!;
            if (IsNumber)
                return scalars.Long.ToString();
            if (IsFloat)
                return scalars.Float.ToString();
            if (IsBoolean)
                return scalars.Boolean.ToString();
            return "null";
        }
    }

    public class ExpressionVisitor : ExpressionTree.Visitor<FilterOutput>
    {
        private PacketViewModel? packet;
        private static IsPacketPlayerProcessor isPlayerProcessor = new();
        private IsPacketSpecificPlayerProcessor isPacketSpecificPlayerProcessor;
        private readonly IPacketViewModelStore store;

        public ExpressionVisitor(IsPacketSpecificPlayerProcessor isPacketSpecificPlayerProcessor,
            IPacketViewModelStore store)
        {
            this.isPacketSpecificPlayerProcessor = isPacketSpecificPlayerProcessor;
            this.store = store;
        }

        public void SetContext(PacketViewModel packet)
        {
            this.packet = packet;
        }

        public override FilterOutput VisitENegate(ExpressionTree.ENegate context)
        {
            var o = Visit(context.Expr);
            if (o.Boolean is { } b)
                return !b;
            throw new Exception("Bool expected in " + context.Text);
        }

        public override FilterOutput VisitEGreaterEquals(ExpressionTree.EGreaterEquals context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.A, context.B);

            return a.HasValue ? a.Value >= b : fa >= fb;
        }

        public (long?, long, float, float) EvalNumbers(ExpressionTree.ITreeNode exprA, ExpressionTree.ITreeNode exprB)
        {
            var a = Visit(exprA);
            var b = Visit(exprB);

            var l1 = a.Long;
            var l2 = b.Long;
            
            var f1 = a.Float;
            var f2 = b.Float;

            if (l1 != null && l2 != null)
                return (l1, l2.Value, 0, 0);
            
            if (f1 != null && l2 != null)
                return (null, 0, f1.Value, l2.Value);
            
            if (f2 != null && l1 != null)
                return (null, 0, l1.Value, f2.Value);
            
            if (f1 != null && f2 != null)
                return (null, 0, f1.Value, f2.Value);
            
            throw new Exception("Number expected in " + exprA.Text + " and " + exprB.Text);
        }
        
        public (string, string) EvalStrings(ExpressionTree.ITreeNode exprA, ExpressionTree.ITreeNode exprB)
        {
            var a = Visit(exprA);
            var b = Visit(exprB);
            if (a.Text is not string l1)
                throw new Exception("String expected in " + exprA.Text);
            if (b.Text is not string l2)
                throw new Exception("String expected in " + exprB.Text);
            return (l1, l2);
        }
        
        public (bool, bool) EvalBools(ExpressionTree.ITreeNode exprA, ExpressionTree.ITreeNode exprB)
        {
            var a = Visit(exprA);
            var b = Visit(exprB);
            if (a.Boolean is not bool l1)
                throw new Exception("Bool expected in " + exprA.Text);
            if (b.Boolean is not bool l2)
                throw new Exception("Bool expected in " + exprB.Text);
            return (l1, l2);
        }

        public override FilterOutput VisitEEquals(ExpressionTree.EEquals context)
        {
            var a = Visit(context.A);
            var b = Visit(context.B);
            if (a.Long is long l1 && b.Long is long l2)
                return l1 == l2;
            if (a.Boolean is bool b1 && b.Boolean is bool b2)
                return b1 == b2;
            if (a.Text is string s1 && b.Text is string s2)
                return s1 == s2;
            throw new Exception($"You cannot compare {a.GetType()} and {b.GetType()}");
        }

        public override FilterOutput VisitELessThan(ExpressionTree.ELessThan context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.A, context.B);
            return a.HasValue ? a.Value < b : fa < fb;
        }

        public override FilterOutput VisitETrue(ExpressionTree.ETrue context)
        {
            return true;
        }

        public override FilterOutput VisitEOr(ExpressionTree.EOr context)
        {
            var (a, b) = EvalBools(context.A, context.B);
            return a || b;
        }

        public override FilterOutput VisitEInt(ExpressionTree.EInt context)
        {
            return context.Value;
        }

        public override FilterOutput VisitEFieldValue(ExpressionTree.EFieldValue context)
        {
            if (this.packet == null)
                return 0;
            var left = context.Identifiers[0];
            var right = context.Identifiers[1];
            if (left == "packet")
            {
                switch (right)
                {
                    case "opcode":
                        return packet.Opcode;
                    case "text":
                        return store.GetText(packet);
                    case "id":
                        return (long) packet.Id;
                    case "original_id":
                    case "originalId":
                        return (long) packet.OriginalId;
                    case "entry":
                        return (long) packet.Entry;
                }
            }
            else
                return left.ToUpper() + "_" + right;
            

            throw new Exception($"Unknown field {left}.{right}");
        }

        public override FilterOutput VisitEIn(ExpressionTree.EIn context)
        {
            var (a, b) = EvalStrings(context.A, context.B);
            return b.Contains(a);
        }

        public override FilterOutput VisitELessEquals(ExpressionTree.ELessEquals context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.A, context.B);
            return a.HasValue ? a.Value <= b : fa <= fb;
        }

        public override FilterOutput VisitEAnd(ExpressionTree.EAnd context)
        {
            var (a, b) = EvalBools(context.A, context.B);
            return a && b;
        }

        public override FilterOutput VisitEMulOp(ExpressionTree.EMulOp context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.A, context.B);
            return a.HasValue ? FilterOutput.FromNumber(a.Value * b) : FilterOutput.FromFloat(fa * fb);
        }

        public override FilterOutput VisitEDivOp(ExpressionTree.EDivOp context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.A, context.B);
            return a.HasValue ? FilterOutput.FromNumber(a.Value / b) : FilterOutput.FromFloat(fa / fb);
        }

        public override FilterOutput VisitEPlusOp(ExpressionTree.EPlusOp context)
        {
            var va = Visit(context.A);
            var vb = Visit(context.B);
            if (va.Text is string s1 && vb.Text is string s2)
                return s1 + s2;
            
            var (a, b, fa, fb) = EvalNumbers(context.A, context.B);
            return a.HasValue ? FilterOutput.FromNumber(a.Value + b) : FilterOutput.FromFloat(fa + fb);
        }

        public override FilterOutput VisitEParen(ExpressionTree.EParen context)
        {
            return Visit(context.Inner);
        }

        public override FilterOutput VisitEFalse(ExpressionTree.EFalse context)
        {
            return false;
        }

        public override FilterOutput VisitEGreaterThan(ExpressionTree.EGreaterThan context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.A, context.B);
            return a.HasValue ? a > b : fa > fb;
        }

        public override FilterOutput VisitENotEquals(ExpressionTree.ENotEquals context)
        {
            var a = Visit(context.A);
            var b = Visit(context.B);
            if (a.Long is long l1 && b.Long is long l2)
                return l1 != l2;
            if (a.Boolean is bool b1 && b.Boolean is bool b2)
                return b1 != b2;
            if (a.Text is string s1 && b.Text is string s2)
                return s1 != s2;
            throw new Exception("You cannot compare not matching types");
        }

        public override FilterOutput VisitEFuncAppl(ExpressionTree.EFuncAppl context)
        {
            var args = context.Args.Select(Visit).ToList();
            var functionName = context.FunctionName;
            if (functionName == "str")
            {
                if (args[0].Float is float f)
                    return f.ToString("0.0#");
                return args[0].ToString()!;
            }
            if (functionName == "int")
            {
                if (args[0].Long is long l)
                    return l;
                else if (args[0].Float is float f)
                    return (long)f;
                else if (args[0].Text is string s && long.TryParse(s, out var num))
                    return num;
                return 0L;
            }
            if (functionName == "lower")
            {
                if (args[0].Text is string l)
                    return l.ToLower();
                throw new Exception("First argument of lower() must be a string!");
            }
            if (functionName == "ceil")
            {
                if (args[0].Long is long l)
                    return l;
                else if (args[0].Float is float f)
                    return (long)Math.Ceiling(f);
                return 0L;
            }
            if (functionName == "is_me")
                return isPacketSpecificPlayerProcessor.Process(ref packet!.Packet);
            if (functionName == "is_player")
                return isPlayerProcessor.Process(ref packet!.Packet);
            throw new Exception($"Unknown function {functionName}");
        }

        public override FilterOutput VisitEStr(ExpressionTree.EStr context)
        {
            return context.Text;
        }
    }
}