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


        if (left is ConstantExpression leftConst && leftConst.Value is bool leftBool && leftBool)
        {
            if (node.NodeType == ExpressionType.AndAlso)
            {
                return Expression.Convert(right, typeof(bool));
            }
        }


        if (right is ConstantExpression rightConst && rightConst.Value is bool rightBool && rightBool)
        {
            if (node.NodeType == ExpressionType.AndAlso)
            {
                return Expression.Convert(left, typeof(bool));
            }
        }


        return Expression.MakeBinary(node.NodeType, left, right);
    }
}
