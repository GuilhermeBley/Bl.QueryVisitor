using Bl.QueryVisitor.MySql.Providers;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.Visitors;

internal class FirstParameterVisitor
    : ExpressionVisitor
{
    private readonly ColumnNameProvider _columnNameProvider;
    private string? _columnName;
    private FirstParameterVisitor(ColumnNameProvider columnNameProvider)
    {
        _columnNameProvider = columnNameProvider;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node.Name is not null)
        {
            _columnName = _columnNameProvider.GetColumnName(node.Name);
            return node;
        }
        
        return base.VisitParameter(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression?.NodeType == ExpressionType.Parameter)
        {
            _columnName = _columnNameProvider.GetColumnName(node.Member.Name);
            return node;
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        _columnName = null;
        return node;
    }

    public static string? GetParameterName(Expression expression, ColumnNameProvider columnNameProvider)
    {
        var translator = new FirstParameterVisitor(columnNameProvider);

        translator.Visit(expression);

        return translator._columnName;
    }
}
