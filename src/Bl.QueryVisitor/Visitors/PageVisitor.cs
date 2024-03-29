using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

public class QueryInfoExtractor : ExpressionVisitor
{
    private int? _skipValue;
    private int? _takeValue;

    public (int? SkipValue, int? TakeValue) GetQueryInfo(Expression expression)
    {
        _skipValue = null;
        _takeValue = null;
        Visit(expression);
        return (_skipValue, _takeValue);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == "Skip")
        {
            if (node.Arguments.Count == 2)
            {
                if (node.Arguments[1] is ConstantExpression constantExpression)
                {
                    _skipValue = (int?)constantExpression.Value ?? throw new ArgumentNullException("skipValue");
                }
            }
        }
        else if (node.Method.Name == "Take")
        {
            if (node.Arguments.Count == 2)
            {
                if (node.Arguments[1] is ConstantExpression constantExpression)
                {
                    _takeValue = (int?)constantExpression.Value ?? throw new ArgumentNullException("takeValue");
                }
            }
        }

        return base.VisitMethodCall(node);
    }
}