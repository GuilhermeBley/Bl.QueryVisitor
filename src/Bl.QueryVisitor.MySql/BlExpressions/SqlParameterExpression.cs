using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.BlExpressions;

public class SqlParameterExpression : BlExpression
{
    private object? _sqlParameter;
    private ConstantExpression _callExpression;

    public override bool CanReduce => true;
    public override ExpressionType NodeType => ExpressionType.Extension;
    public object? SqlParameter => _sqlParameter;
    public override Type Type => _callExpression.Type;

    public SqlParameterExpression(object? parameter, ConstantExpression callExpression)
    {
        _sqlParameter = parameter;
        _callExpression = callExpression;
    }

    public override string ToString()
    {
        return _callExpression.ToString();
    }

    public override Expression Reduce()
    {
        return _callExpression;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        var exp = visitor.Visit(_callExpression);

        if (exp is not ConstantExpression c) return this;

        return new SqlParameterExpression(_sqlParameter, c);
    }
}
