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
                return VisitConstant(Expression.Constant(value, node.Type));
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
                    return VisitConstant(Expression.Constant(instance.ToString(), typeof(string)));
                }

                // More general case - try to invoke the method
                var result = node.Method.Invoke(instance, argValues);
                return VisitConstant(Expression.Constant(result, node.Type));
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

        var leftType = GetTrueExpressionType(left);
        var rightType = GetTrueExpressionType(right);

        if (left.Type != right.Type)
        {
            var leftUnderlying = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
            var rightUnderlying = Nullable.GetUnderlyingType(right.Type) ?? right.Type;

            if (leftUnderlying == rightUnderlying)
            {
                if (Nullable.GetUnderlyingType(left.Type) == null)
                {
                    left = Expression.Convert(left, right.Type);
                }
                else
                {
                    right = Expression.Convert(right, left.Type);
                }
            }
        }

        return Expression.MakeBinary(
            node.NodeType, 
            left: left, 
            right: right);
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
                    ? null
                    : node.NodeType switch
                    {
                        ExpressionType.Convert => ExecuteConst(node),
                        ExpressionType.Not => !(bool)constOperand.Value,
                        ExpressionType.Negate => -(Convert.ToDouble(constOperand.Value)),
                        _ => throw new NotSupportedException($"Unary operator {node.NodeType} not supported")
                    };
                return VisitConstant(Expression.Constant(result, node.Type));
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

    private static Type GetTrueExpressionType(Expression expression)
    {
        while (true)
        {
            switch (expression)
            {
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert:
                    expression = unary.Operand;
                    continue;

                case MemberExpression member:
                    return member.Member is PropertyInfo prop ? prop.PropertyType :
                           ((FieldInfo)member.Member).FieldType;

                case MethodCallExpression methodCall:
                    return methodCall.Method.ReturnType;

                case ConditionalExpression conditional:
                    return conditional.Type;

                case BinaryExpression binary:
                    return binary.Type;

                case LambdaExpression lambda:
                    return lambda.ReturnType;

                case InvocationExpression invocation:
                    return invocation.Type;

                case ConstantExpression constexp:
                    return constexp.Type;

                default:
                    return expression.Type;
            }
        }
    }
}
