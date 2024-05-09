using System;
using System.Linq;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Expressions.Antlr
{
    public class ExpressionVisitor : DatabaseEditorExpressionBaseVisitor<object>
    {
        private DatabaseEntity? entity;
        private readonly ICreatureStatCalculatorService statCalculatorService;
        private readonly IParameterFactory parameterFactory;
        private readonly DatabaseTableDefinitionJson definition;

        public ExpressionVisitor(ICreatureStatCalculatorService statCalculatorService,
            IParameterFactory parameterFactory,
            DatabaseTableDefinitionJson definition)
        {
            this.statCalculatorService = statCalculatorService;
            this.parameterFactory = parameterFactory;
            this.definition = definition;
        }

        public void SetContext(DatabaseEntity entity)
        {
            this.entity = entity;
        }
        
        public override object VisitENegate(DatabaseEditorExpressionParser.ENegateContext context)
        {
            var o = Visit(context.expr());
            if (o is bool b)
                return !b;
            throw new Exception();
        }

        public override object VisitEGreaterEquals(DatabaseEditorExpressionParser.EGreaterEqualsContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a.HasValue ? a.Value >= b : fa >= fb;
        }

        public (long?, long, float, float) EvalNumbers(DatabaseEditorExpressionParser.ExprContext[] expr)
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
            
            throw new Exception();
        }
        
        public (bool, bool) EvalBools(DatabaseEditorExpressionParser.ExprContext[] expr)
        {
            var a = Visit(expr[0]);
            var b = Visit(expr[1]);
            if (a is not bool l1)
                throw new Exception();
            if (b is not bool l2)
                throw new Exception();
            return (l1, l2);
        }

        public override object VisitEEquals(DatabaseEditorExpressionParser.EEqualsContext context)
        {
            var a = Visit(context.expr()[0]);
            var b = Visit(context.expr()[1]);
            if (a is long l1 && b is long l2)
                return l1 == l2;
            if (a is bool b1 && b is bool b2)
                return b1 == b2;
            throw new Exception();
        }

        public override object VisitELessThan(DatabaseEditorExpressionParser.ELessThanContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a.HasValue ? a.Value < b : fa < fb;
        }

        public override object VisitETrue(DatabaseEditorExpressionParser.ETrueContext context)
        {
            return true;
        }

        public override object VisitEOr(DatabaseEditorExpressionParser.EOrContext context)
        {
            var (a, b) = EvalBools(context.expr());
            return a || b;
        }

        public override object VisitEInt(DatabaseEditorExpressionParser.EIntContext context)
        {
            if (!long.TryParse(context.INT().GetText(), out var asLong))
                throw new Exception();
            return asLong;
        }

        public override object VisitEFieldStringValue(DatabaseEditorExpressionParser.EFieldStringValueContext context)
        {
            if (context.ID().GetText() is not { } columnName)
                return "(unknown)";

            var name = new ColumnFullName(null, columnName);
            if (!definition.TableColumns.TryGetValue(name, out var column))
                return "(unknown)";
            
            var cell = entity?.GetCell(name);
            if (cell is DatabaseField<long> lField)
                return parameterFactory.Factory(column.ValueType).ToString(lField.Current.Value, new ToStringOptions(){withNumber = false});
            
            if (cell is DatabaseField<float> fField)
                return fField.Current.ToString();

            if (cell is DatabaseField<string> sField)
            {
                var sParam = parameterFactory.FactoryString(column.ValueType);
                if (sParam is IContextualParameter<string, DatabaseEntity> sContext)
                    return sContext.ToString(sField.Current.Value ?? "", entity!);
                return sParam.ToString(sField.Current.Value ?? "", new ToStringOptions(){withNumber = false});
            }
            
            return "";
        }

        public override object VisitEFieldValue(DatabaseEditorExpressionParser.EFieldValueContext context)
        {
            var name = context.ID().GetText();
            if (name == null)
                throw new Exception();
            var cell = entity?.GetCell(new ColumnFullName(null, name));
            if (cell is DatabaseField<long> lField)
                return lField.Current.Value;
            if (cell is DatabaseField<float> fField)
                return fField.Current.Value;
            if (cell is DatabaseField<string> sField)
                return sField.Current.Value ?? "";
            return 0L;
        }

        public override object VisitELessEquals(DatabaseEditorExpressionParser.ELessEqualsContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a.HasValue ? a.Value <= b : fa <= fb;
        }

        public override object VisitEAnd(DatabaseEditorExpressionParser.EAndContext context)
        {
            var (a, b) = EvalBools(context.expr());
            return a && b;
        }

        public override object VisitEMulOp(DatabaseEditorExpressionParser.EMulOpContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a * b ?? fa * fb;
        }

        public override object VisitEDivOp(DatabaseEditorExpressionParser.EDivOpContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a / b ?? fa / fb;
        }

        public override object VisitEPlusOp(DatabaseEditorExpressionParser.EPlusOpContext context)
        {
            var va = Visit(context.expr()[0]);
            var vb = Visit(context.expr()[1]);
            if (va is string s1 && vb is string s2)
                return s1 + s2;
            
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a + b ?? fa + fb;
        }

        public override object VisitEParen(DatabaseEditorExpressionParser.EParenContext context)
        {
            return Visit(context.expr());
        }

        public override object VisitEFalse(DatabaseEditorExpressionParser.EFalseContext context)
        {
            return false;
        }

        public override object VisitEGreaterThan(DatabaseEditorExpressionParser.EGreaterThanContext context)
        {
            var (a, b, fa, fb) = EvalNumbers(context.expr());
            return a.HasValue ? a > b : fa > fb;
        }

        public override object VisitENotEquals(DatabaseEditorExpressionParser.ENotEqualsContext context)
        {
            var a = Visit(context.expr()[0]);
            var b = Visit(context.expr()[1]);
            if (a is long l1 && b is long l2)
                return l1 != l2;
            if (a is bool b1 && b is bool b2)
                return b1 != b2;
            throw new Exception();
        }

        public override object VisitEFuncAppl(DatabaseEditorExpressionParser.EFuncApplContext context)
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
            if (functionName == "ceil")
            {
                if (args[0] is long l)
                    return l;
                else if (args[0] is float f)
                    return (long)Math.Ceiling(f);
                return 0L;
            }
            if (functionName == "GetHP")
            {
                return (long)statCalculatorService.GetHealthFor((byte) (long) args[0], (byte) (long) args[1],
                    (byte) (long) args[2]);
            }
            
            if (functionName == "GetMana")
            {
                return (long)statCalculatorService.GetManaFor((byte) (long) args[0], (byte) (long) args[1]);
            }
            
            if (functionName == "GetArmor")
            {
                return (long)statCalculatorService.GetArmorFor((byte) (long) args[0], (byte) (long) args[1]);
            }
            
            if (functionName == "GetAPBonus")
            {
                return (long)statCalculatorService.GetAttackPowerBonusFor((byte) (long) args[0], (byte) (long) args[1]);
            }
            
            if (functionName == "GetRangeAPBonus")
            {
                return (long)statCalculatorService.GetRangedAttackPowerBonusFor((byte) (long) args[0], (byte) (long) args[1]);
            }

            if (functionName == "GetDamage")
            {
                return (float)statCalculatorService.GetDamageFor((byte) (long) args[0], (byte) (long) args[1],
                    (byte) (long) args[2]);
            }
            throw new Exception($"Unknown function {functionName}");
        }

        public override object VisitEStr(DatabaseEditorExpressionParser.EStrContext context)
        {
            var text = context.STRING().GetText() ?? "''";
            return text.Substring(1, text.Length - 2);
        }
    }
}