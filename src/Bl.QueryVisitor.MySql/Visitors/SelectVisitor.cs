using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

internal class SelectVisitor : ExpressionVisitor
{
    public static readonly ICollection<Type> AllowedTypes 
        = new HashSet<Type>
        {
            typeof(Guid),
            typeof(DateTimeOffset),
            typeof(DateOnly),
        };

    private readonly List<string> _columns = new List<string>();

    public SelectVisitor()
    {

    }

    public IEnumerable<string> TranslateColumns(Expression expression)
    {
        _columns.Clear();
        Visit(expression);
        return _columns.ToArray();
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        var lambda = node as LambdaExpression;
        
        // Compile the expression to a delegate
        var compiledDelegate = lambda.Compile();

        // Create a new Func<object, object> that wraps the compiled delegate
        Func<object?, object?> func = (input) =>
        {
            // Convert input to the type expected by the original delegate
            var typedInput = input;

            // Invoke the compiled delegate
            var result = compiledDelegate.DynamicInvoke(typedInput);

            // Return the result as an object
            return result;
        };

        return base.VisitLambda(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (!CanBeSelected(node.Type))
            return base.VisitMember(node);

        _columns.Add(node.Member.Name);
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
