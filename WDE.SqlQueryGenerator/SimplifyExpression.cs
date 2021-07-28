using System;
using System.Linq.Expressions;

namespace WDE.SqlQueryGenerator
{
    internal class SimplifyExpression : ExpressionVisitor
    {
        public bool EvaluateBool(Expression e, out bool result)
        {
            result = false;
            if (!Evaluate(e, out var objres))
                return false;
            if (objres is bool b)
            {
                result = b;
                return true;
            }

            return false;
        }
        
        public bool Evaluate(Expression e, out object? result)
        {
            try
            {
                if (e.NodeType == ExpressionType.Constant)
                    result = ((ConstantExpression)e).Value;
                else
                    result = Expression.Lambda(e).Compile().DynamicInvoke();
                
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }
        
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var op = node.NodeType;
            if (op == ExpressionType.OrElse)
            {
                var leftEvaluation = EvaluateBool(Visit(node.Left), out var left);
                var rightEvaluation = EvaluateBool(Visit(node.Right), out var right);

                if (leftEvaluation && rightEvaluation)
                    return Expression.Constant(left || right);
                
                if (leftEvaluation)
                    return left ? Expression.Constant(true) : Visit(node.Right);
                
                if (rightEvaluation)
                    return right ? Expression.Constant(true) : Visit(node.Left);

                return node.Update(Visit(node.Left), node.Conversion, Visit(node.Right));
            }
            else if (op == ExpressionType.AndAlso)
            {
                var leftEvaluation = EvaluateBool(Visit(node.Left), out var left);
                var rightEvaluation = EvaluateBool(Visit(node.Right), out var right);

                if (leftEvaluation && rightEvaluation)
                    return Expression.Constant(left && right);
                
                if (leftEvaluation)
                    return left ? Visit(node.Right) : Expression.Constant(false);
                
                if (rightEvaluation)
                    return right ? Visit(node.Left) : Expression.Constant(false);

                return node.Update(Visit(node.Left), node.Conversion, Visit(node.Right));
            }
            else
            {
                var leftEvaluation = Evaluate(Visit(node.Left), out var left);
                var rightEvaluation = Evaluate(Visit(node.Right), out var right);
                return node.Update(leftEvaluation ? Expression.Constant(left) : node.Left, node.Conversion ,
                    rightEvaluation ? Expression.Constant(right) : node.Right);
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Column" && node.Arguments.Count == 1 &&
                node.Method.ReturnType == typeof(bool) &&
                Visit(node.Arguments[0]) is ConstantExpression arg1)
            {
                return Expression.Equal(node, Expression.Constant(true));
            }
            return base.VisitMethodCall(node);
        }
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (Evaluate(node, out var result))
                return Expression.Constant(result);

            if (node.NodeType == ExpressionType.Not)
            {
                var unary = Visit(node.Operand);
                if (unary is BinaryExpression be && be.NodeType == ExpressionType.Equal)
                    return Expression.NotEqual(be.Left, be.Right);
            }
            return base.VisitUnary(node);
        }
    }
}