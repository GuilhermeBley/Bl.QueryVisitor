using Bl.QueryVisitor.MySql.Providers;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.Visitors;

internal class BinarySimplificatorVisitor
{
    public BinaryExpression ParseBinary(BinaryExpression node)
        => TrySimplifyToCompareAndSetNewExpression(node)
        ?? node;

    private BinaryExpression? TrySimplifyToCompareAndSetNewExpression(BinaryExpression binaryExpression)
    {
        if (binaryExpression.Left is not MethodCallExpression member)
            return null;

        if (member.Method.Name != "Compare" || member.Arguments.Count != 2 || member.Arguments[0] is not MemberExpression)
            return null;

        var newExpression =
            Expression.MakeBinary(
                binaryExpression.NodeType,
                left: member.Arguments[0],
                right: member.Arguments[1],
                false,
                member.Method);

        return newExpression;
    }
}
