using Bl.QueryVisitor.MySql.BlExpressions;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.Visitors;

internal class ConditionalExpressionVisitorSimplifier
    : ExpressionVisitor
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
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType == ExpressionType.AndAlso && left is ConstantExpression leftConst && leftConst.Value is bool)
        {
            return right;
        }

        if (node.NodeType == ExpressionType.AndAlso && right is ConstantExpression rightConst && rightConst.Value is bool)
        {
            return left;
        }

        return Expression.MakeBinary(node.NodeType, left, right);
    }
}
