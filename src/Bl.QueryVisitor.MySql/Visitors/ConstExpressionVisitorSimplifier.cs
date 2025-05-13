using Bl.QueryVisitor.MySql.BlExpressions;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace Bl.QueryVisitor.MySql.Visitors;

internal class ConstExpressionVisitorSimplifier : ExpressionVisitor
{
    protected override Expression VisitExtension(Expression node)
    {
        // don't override any 'BlExpression'
        if (node is BlExpression)
        {
            return node;
        }

        return base.VisitExtension(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // Try to evaluate member access on constants
        var expression = Visit(node.Expression);
        if (expression is ConstantExpression constantExpression)
        {
            try
            {
                var value = node.Member switch
                {
                    FieldInfo fi => fi.GetValue(constantExpression.Value),
                    PropertyInfo pi => pi.GetValue(constantExpression.Value),
                    _ => throw new NotSupportedException($"Member type {node.Member.MemberType} not supported")
                };
                return Expression.Constant(value, node.Type);
            }
            catch
            {
                // If evaluation fails, keep the original expression
                return node;
            }
        }
        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Try to evaluate method calls on constants
        var obj = node.Object != null ? Visit(node.Object) : null;
        var args = node.Arguments.Select(Visit).ToArray();

        // If all arguments are constant (including the instance for instance methods)
        if ((obj == null || obj is ConstantExpression) && args.All(a => a is ConstantExpression))
        {
            try
            {
                var instance = obj is ConstantExpression constObj ? constObj.Value : null;
                var argValues = args.Select(a => ((ConstantExpression?)a)?.Value).ToArray();

                // Handle common methods we want to evaluate at compile time
                if (node.Method.Name == "ToString" && instance != null)
                {
                    return Expression.Constant(instance.ToString(), typeof(string));
                }

                // More general case - try to invoke the method
                var result = node.Method.Invoke(instance, argValues);
                return Expression.Constant(result, node.Type);
            }
            catch
            {
                // If evaluation fails, keep the original expression
                return base.VisitMethodCall(node);
            }
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // If both sides are constant, evaluate the operation
        if (left is ConstantExpression leftConst && right is ConstantExpression rightConst)
        {
            try
            {
                object result = node.NodeType switch
                {
                    ExpressionType.Add => Convert.ToDouble(leftConst.Value) + Convert.ToDouble(rightConst.Value),
                    ExpressionType.Subtract => Convert.ToDouble(leftConst.Value) - Convert.ToDouble(rightConst.Value),
                    ExpressionType.Multiply => Convert.ToDouble(leftConst.Value) * Convert.ToDouble(rightConst.Value),
                    ExpressionType.Divide => Convert.ToDouble(leftConst.Value) / Convert.ToDouble(rightConst.Value),
                    ExpressionType.Equal => Equals(leftConst.Value, rightConst.Value),
                    ExpressionType.NotEqual => !Equals(leftConst.Value, rightConst.Value),
                    ExpressionType.GreaterThan => Convert.ToDouble(leftConst.Value) > Convert.ToDouble(rightConst.Value),
                    ExpressionType.GreaterThanOrEqual => Convert.ToDouble(leftConst.Value) >= Convert.ToDouble(rightConst.Value),
                    ExpressionType.LessThan => Convert.ToDouble(leftConst.Value) < Convert.ToDouble(rightConst.Value),
                    ExpressionType.LessThanOrEqual => Convert.ToDouble(leftConst.Value) <= Convert.ToDouble(rightConst.Value),
                    ExpressionType.AndAlso => (bool)leftConst.Value! && (bool)rightConst.Value!,
                    ExpressionType.OrElse => (bool)leftConst.Value! || (bool)rightConst.Value!,
                    _ => throw new NotSupportedException($"Binary operator {node.NodeType} not supported")
                };
                return Expression.Constant(result, typeof(bool)); // Most comparisons return bool
            }
            catch
            {
                return Expression.MakeBinary(node.NodeType, left, right);
            }
        }

        return Expression.MakeBinary(node.NodeType, left, right);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);

        if (operand is ConstantExpression constOperand)
        {
            try
            {
                object? result = 
                    constOperand.Value is null
                    ? Expression.Constant(null)
                    : node.NodeType switch
                    {
                        ExpressionType.Convert => ExecuteConst(node),
                        ExpressionType.Not => !(bool)constOperand.Value,
                        ExpressionType.Negate => -(Convert.ToDouble(constOperand.Value)),
                        _ => throw new NotSupportedException($"Unary operator {node.NodeType} not supported")
                    };
                return Expression.Constant(result, node.Type);
            }
            catch(Exception e)
            {
                Debug.WriteLine($"Failed to convert node {node.Type} with value {constOperand.Value}. Error: {e.Message}");
                return base.VisitUnary(node);
            }
        }

        return base.VisitUnary(node);
    }

    private object? ExecuteConst(UnaryExpression u)
    {
        var convertedExp = Expression.Convert(u, typeof(object));

        var instantiator = Expression
            .Lambda<Func<object>>(convertedExp)
            .Compile();
        var res = instantiator();

        return res;
    }
}
