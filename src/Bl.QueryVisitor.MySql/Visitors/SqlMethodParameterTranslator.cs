using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.Visitors;

internal class SqlMethodParameterTranslator
    : ExpressionVisitor
{
    private static readonly IReadOnlyDictionary<string, string> _sqlFunctions
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {nameof(DateTime.Day), "Day"},
            {nameof(DateTime.Month), "Month"},
            {nameof(DateTime.Year), "Year"},
            {nameof(DateTime.DayOfWeek), "Dayofweek"},
            {nameof(DateTime.DayOfYear), "Dayofyear"},
            {nameof(DateTime.Hour), "Hour"},
            {nameof(DateTime.Minute), "Minute"},
            {nameof(DateTime.Second), "Second"},
        };

    private IReadOnlyDictionary<string, string> _renamedProperties;
    private string? _functionName;

    public SqlMethodParameterTranslator(IReadOnlyDictionary<string, string> renamedProperties)
    {
        _renamedProperties = renamedProperties;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var functionName = node.Member.Name;

        if (node.Expression is MemberExpression funcMember &&
            _sqlFunctions.TryGetValue(functionName, out var sqlFunction))
        {
            var fieldName = funcMember.Member.Name;

            var columnName = _renamedProperties
                .TryGetValue(fieldName, out var renamedValue)
                    ? renamedValue
                    : fieldName;

            _functionName = string.Concat(
                sqlFunction,
                '(',
                columnName,
                ')'
            );

            return node;
        }

        return Visit(node);
    }

    public static bool TryTranslate(
        Expression node,
        IReadOnlyDictionary<string, string> renamedProperties,
        out string? translatedFunctionName)
    {
        var visitor = new SqlMethodParameterTranslator(renamedProperties);

        visitor.Visit(node);

        translatedFunctionName = visitor._functionName;

        return translatedFunctionName is not null;
    }
}
