using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.Api.Test.Controllers
{
    public class ODataExpressionVisitorSimplifier : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            // Simplify binary expressions (e.g., &&, ||, ==, etc.)
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            // Example: Simplify "true && x" to "x"
            if (node.NodeType == ExpressionType.AndAlso && left is ConstantExpression leftConst && (bool)leftConst.Value)
            {
                return right;
            }

            // Example: Simplify "x && true" to "x"
            if (node.NodeType == ExpressionType.AndAlso && right is ConstantExpression rightConst && (bool)rightConst.Value)
            {
                return left;
            }

            // If no simplification is applied, return the original node
            return Expression.MakeBinary(node.NodeType, left, right);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            // Simplify unary expressions (e.g., !, -, etc.)
            var operand = Visit(node.Operand);

            // Example: Simplify "!!x" to "x"
            if (node.NodeType == ExpressionType.Not && operand is UnaryExpression innerUnary && innerUnary.NodeType == ExpressionType.Not)
            {
                return innerUnary.Operand;
            }

            // If no simplification is applied, return the original node
            return Expression.MakeUnary(node.NodeType, operand, node.Type);
        }
    }
}
