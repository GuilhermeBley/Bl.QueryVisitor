using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.Visitors;

internal class ConditionalExpressionVisitorSimplifier
    : ExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType == ExpressionType.AndAlso && left is ConstantExpression leftConst && (bool)leftConst.Value)
        {
            return right;
        }

        if (node.NodeType == ExpressionType.AndAlso && right is ConstantExpression rightConst && (bool)rightConst.Value)
        {
            return left;
        }

        return Expression.MakeBinary(node.NodeType, left, right);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);

        if (node.NodeType == ExpressionType.Not && operand is UnaryExpression innerUnary && innerUnary.NodeType == ExpressionType.Not)
        {
            return innerUnary.Operand;
        }

        return Expression.MakeUnary(node.NodeType, operand, node.Type);
    }
}
