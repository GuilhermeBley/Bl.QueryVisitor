using Bl.QueryVisitor.MySql;
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
    private readonly Type _modelType;

    public IReadOnlyCollection<string> Columns => _columns;
    public bool ColumnsAlreadyTranslated => _columnsAlreadyTranslated;

    public SelectVisitor(Type modelType)
    {
        _modelType = modelType;
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
        if (!ShouldTranslate())
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

    public bool ShouldTranslate()
    {
        return _transformations.Count != 0;
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
        if (node.Member.DeclaringType != _modelType)
            return base.VisitMember(node);
        _columns.Add(node.Member.Name);
        _anyColumnAlreadyTranslated = true;
        return base.VisitMember(node);
    }
}
