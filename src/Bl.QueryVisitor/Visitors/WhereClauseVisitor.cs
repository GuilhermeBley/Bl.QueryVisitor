using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

public class WhereClauseVisitor : ExpressionVisitor
{
    private readonly List<WhereClauseInfo> _whereClauses = new List<WhereClauseInfo>();

    public List<WhereClauseInfo> GetWhereClauses(Expression expression)
    {
        _whereClauses.Clear();
        Visit(expression);
        return _whereClauses;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name != "Where")
            return base.VisitMethodCall(node);

        if (node.Arguments.Count == 2 && node.Arguments[1] is LambdaExpression lambdaExpression)
        {
            var body = lambdaExpression.Body;
            var propertyName = ((MemberExpression) ((BinaryExpression) body).Left).Member.Name;
            var constant = ((BinaryExpression) body).Right as ConstantExpression;
            var value = constant?.Value;

            if (constant != null)
            {
                var comparer = (BinaryExpression) node.Arguments[1];
                var comparerType = comparer.Method?.DeclaringType?.Name;
                var comparerMethod = comparer.Method?.Name;

                ArgumentNullException.ThrowIfNull(comparerType);
                ArgumentNullException.ThrowIfNull(comparerMethod);

                _whereClauses.Add(new WhereClauseInfo(propertyName, comparerType, comparerMethod, value));
            }
        }

        return base.VisitMethodCall(node);
    }
}

public record WhereClauseInfo(
    string PropertyName,
    string ComparerType,
    string ComparerMethod,
    object? Value
);