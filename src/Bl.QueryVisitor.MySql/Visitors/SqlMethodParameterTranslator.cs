using Bl.QueryVisitor.MySql.Providers;
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

    private readonly ColumnNameProvider _columnNameProvider;
    private string? _functionName;

    public SqlMethodParameterTranslator(ColumnNameProvider columnNameProvider)
    {
        _columnNameProvider = columnNameProvider;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var functionName = node.Member.Name;

        if (node.Expression is MemberExpression funcMember &&
            _sqlFunctions.TryGetValue(functionName, out var sqlFunction))
        {
            var fieldName = funcMember.Member.Name;

            var columnName = _columnNameProvider.GetColumnName(fieldName);

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
        ColumnNameProvider columnNameProvider,
        out string? translatedFunctionName)
    {
        var visitor = new SqlMethodParameterTranslator(columnNameProvider);

        visitor.Visit(node);

        translatedFunctionName = visitor._functionName;

        return translatedFunctionName is not null;
    }
}
