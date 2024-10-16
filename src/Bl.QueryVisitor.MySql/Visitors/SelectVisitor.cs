﻿using Bl.QueryVisitor.MySql;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

internal class SelectVisitor 
    : ExpressionVisitor,
    IItemTranslator
{
    public static readonly ICollection<Type> AllowedTypes 
        = new HashSet<Type>
        {
            typeof(Guid),
            typeof(DateTimeOffset),
            typeof(DateOnly),
        };

    private bool _columnsAlreadyTranslated = false;
    private bool _anyColumnAlreadyTranslated = false;
    private readonly HashSet<string> _columns = new();
    private readonly List<Func<object?, object?>> _transformations = new();

    public IReadOnlyCollection<string> Columns => _columns;
    public bool ColumnsAlreadyTranslated => _columnsAlreadyTranslated;

    public SelectVisitor()
    {
    }

    public IEnumerable<string> TranslateColumns(Expression expression)
    {
        _anyColumnAlreadyTranslated = false;

        Visit(expression);

        if (_anyColumnAlreadyTranslated && _columnsAlreadyTranslated)
            throw new InvalidOperationException("You can only translate the columns once.");

        _columnsAlreadyTranslated = _anyColumnAlreadyTranslated;
        return _columns.ToArray();
    }

    public object? TransformItem(object? input)
    {
        if (_transformations.Count == 0)
            return input;

        try
        {
            object? result = input;

            foreach (var transform in _transformations)
            {
                result = transform(input);
            }

            return result;
        }
        catch
        {
            throw;
        }
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        var lambda = node as LambdaExpression;
        
        var compiledDelegate = lambda.Compile();

        Func<object?, object?> func = (input) =>
        {
            var typedInput = input;

            var result = compiledDelegate.DynamicInvoke(typedInput);

            return result;
        };

        _transformations.Add(func);

        return base.VisitLambda(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (!CanBeSelected(node.Type))
            return base.VisitMember(node);

        _columns.Add(node.Member.Name);
        _anyColumnAlreadyTranslated = true;
        return base.VisitMember(node);
    }

    private static bool CanBeSelected(Type type)
    {
        type = Nullable.GetUnderlyingType(type)
            ?? type;

        if (Type.GetTypeCode(type) != TypeCode.Object)
            return true;

        if (type.IsEnum)
            return true;

        if (AllowedTypes.Contains(type))
            return true;

        return false;
    }
}
