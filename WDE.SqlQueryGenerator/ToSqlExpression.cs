using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace WDE.SqlQueryGenerator
{
    internal class ToSqlExpression : ExpressionVisitor
    {
        /**
         * Returns mysql equivalent operator priority
         * https://dev.mysql.com/doc/refman/8.0/en/operator-precedence.html
         */
        private int GetPriority(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.ArrayLength:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Coalesce:
                case ExpressionType.Call:
                case ExpressionType.Conditional:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Invoke:
                case ExpressionType.Lambda:
                case ExpressionType.ListInit:
                case ExpressionType.MemberAccess:
                case ExpressionType.MemberInit:
                case ExpressionType.New:
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.Parameter:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.TypeIs:
                case ExpressionType.Assign:
                case ExpressionType.Block:
                case ExpressionType.DebugInfo:
                case ExpressionType.Decrement:
                case ExpressionType.Dynamic:
                case ExpressionType.Default:
                case ExpressionType.Extension:
                case ExpressionType.Goto:
                case ExpressionType.Increment:
                case ExpressionType.Index:
                case ExpressionType.Label:
                case ExpressionType.RuntimeVariables:
                case ExpressionType.Loop:
                case ExpressionType.Switch:
                case ExpressionType.Throw:
                case ExpressionType.Try:
                case ExpressionType.Unbox:
                case ExpressionType.AddAssign:
                case ExpressionType.AndAssign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.TypeEqual:
                    throw new Exception();
                case ExpressionType.Constant:
                    return 0;
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.UnaryPlus:
                case ExpressionType.OnesComplement:
                    return 1;
                case ExpressionType.ExclusiveOr:
                    return 2;
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return 3;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return 4;
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                    return 5;
                case ExpressionType.And:
                    return 6;
                case ExpressionType.Or:
                    return 7;
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.NotEqual:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                    return 8;
                case ExpressionType.Not:
                    return 9;
                case ExpressionType.AndAlso:
                    return 10;
                case ExpressionType.OrElse:
                    return 11;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private bool IsExpressionBinaryWithLowerPriority(Expression expr, ExpressionType type)
        {
            return expr is BinaryExpression be && GetPriority(be.NodeType) > GetPriority(type);
        }

        private bool SimplifyConstantExpression(ConstantExpression exp, out string value)
        {
            if (exp.Value is string s)
            {
                value = s;
                return true;
            }

            if (exp.Value is string[] arr && arr.Length == 1)
            {
                value = arr[0];
                return true;
            }

            value = null!;
            return false;
        }
        
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            if (left is ConstantExpression lConst && lConst.Value is string[] array &&
                right is ConstantExpression rConst && rConst.Value is string s && int.TryParse(s, out var index))
            {
                return Expression.Constant(array[index]);
            }
            
            if (left is not ConstantExpression lc || !SimplifyConstantExpression(lc, out var ls))
                throw new Exception();
            if (right is not ConstantExpression rc || !SimplifyConstantExpression(rc, out var rs))
                throw new Exception();

            var op = OperatorToSql(node);
            if (IsExpressionBinaryWithLowerPriority(node.Left, node.NodeType))
                ls = $"({ls})";
            if (IsExpressionBinaryWithLowerPriority(node.Right, node.NodeType))
                rs = $"({rs})";
            return Expression.Constant($"{ls} {op} {rs}");
        }

        private static string OperatorToSql(BinaryExpression node)
        {
            var op = node.NodeType switch
            {
                ExpressionType.OrElse => "OR",
                ExpressionType.AndAlso => "AND",
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.Add => "+",
                ExpressionType.AddChecked => "+",
                ExpressionType.And => "&",
                ExpressionType.Divide => "/",
                ExpressionType.ExclusiveOr => "^",
                ExpressionType.Modulo => "%",
                ExpressionType.Multiply => "*",
                ExpressionType.MultiplyChecked => "*",
                ExpressionType.Negate => "-",
                ExpressionType.UnaryPlus => "+",
                ExpressionType.NegateChecked => "-",
                ExpressionType.Not => "NOT",
                ExpressionType.Or => "|",
                ExpressionType.Power => "^",
                ExpressionType.RightShift => ">>",
                ExpressionType.Subtract => "-",
                ExpressionType.SubtractChecked => "-",
                ExpressionType.OnesComplement => "~",
                ExpressionType.LeftShift => "<<",
                _ => throw new ArgumentOutOfRangeException()
            };
            return op;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (disableWrapSql && node.Value != null)
                return Expression.Constant(node.Value.ToString() ?? "");
            return Expression.Constant($"{node.Value.ToSql()}");
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            disableWrapSql = true;
            if (node.Method.Name == "Column" && node.Arguments.Count == 1 &&
                Evaluate(node.Arguments[0], out var arg1))
            {
                disableWrapSql = false;
                Debug.Assert(arg1 != null);
                return Expression.Constant($"`{arg1.ToString()}`");
            }

            if (node.Method.Name == "Variable" && node.Arguments.Count == 1 &&
                Evaluate(node.Arguments[0], out var arg2))
            {
                disableWrapSql = false;
                Debug.Assert(arg2 != null);
                return Expression.Constant($"@{arg2}");
            }
            else if (node.Method.Name == "Raw" && node.Arguments.Count == 1 &&
                     Evaluate(node.Arguments[0], out var arg3))
            {
                disableWrapSql = false;
                Debug.Assert(arg3 != null);
                return Expression.Constant($"{arg3}");
            }

            throw new Exception("Not sure what to do here: return base.VisitMethodCall(node);?");
        }

        public static bool Evaluate(Expression e, out object? result)
        {
            try
            {
                result = Expression.Lambda(e).Compile().DynamicInvoke();
                
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        private bool disableWrapSql;
        protected override Expression VisitMember(MemberExpression node)
        {
            if (Evaluate(node, out var result))
                return Expression.Constant(disableWrapSql ? result : result.ToSql());
            
            throw new Exception("Not sure what to do here: return base.VisitMember(node);?");
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            //if (Evaluate(node, out var result))
            //    return Expression.Constant(disableWrapSql ? result : result.ToSql());
            if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
                return Visit(node.Operand);
            if (node.NodeType == ExpressionType.Not)
            {
                var operand = Visit(node.Operand);

                if (operand is not ConstantExpression c || c.Value is not string s)
                    throw new Exception();

                if (!IsExpressionBinaryWithLowerPriority(node.Operand, node.NodeType))
                    s = $"({s})";

                if (node.Type == typeof(bool))
                    return Expression.Constant($"NOT {s}");
                return Expression.Constant($"~{s}");   
            }
            
            throw new Exception("Not sure what to do here: return base.VisitUnary(node);?");
        }
    }
}