﻿using Bl.QueryVisitor.MySql.Providers;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

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

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return base.VisitParameter(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var functionName = node.Member.Name;

        if (node.Expression is MemberExpression funcMember &&
            _sqlFunctions.TryGetValue(functionName, out var sqlFunction))
        {
            var columnName = FirstParameterVisitor.GetParameterName(funcMember, _columnNameProvider);

            if (columnName is null)
            {
                //
                // Can't get the property name
                //
                return node;
            }

            _functionName = string.Concat(
                sqlFunction,
                '(',
                columnName,
                ')'
            );

            return node;
        }

        return node;
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
