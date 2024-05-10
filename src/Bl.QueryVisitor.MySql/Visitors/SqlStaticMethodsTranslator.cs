using System.Linq.Expressions;
using System.Net.Http.Headers;

namespace Bl.QueryVisitor.Visitors;

internal class SqlStaticMethodsTranslator
    : ExpressionVisitor
{
    private readonly static IReadOnlyDictionary<string, string> _sqlMethods
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Now", "NOW()" },
            { "UtcNow", "UTC_TIMESTAMP()" },
            { "NewGuid", "UUID()" },
        };

    private string? _currentMethodFound;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var key = node.Method.Name;

        _sqlMethods.TryGetValue(key, out _currentMethodFound);

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var key = node.Member.Name;

        _sqlMethods.TryGetValue(key, out _currentMethodFound);

        return node;
    }

    private bool InternTryTranslate(Expression expression, out string? sqlMethodFound)
    {
        Visit(expression);

        sqlMethodFound = _currentMethodFound;

        return !string.IsNullOrWhiteSpace(sqlMethodFound);
    }

    public static bool TryTranslate(Expression expression, out string? sqlMethodFound)
    {
        var translator = new SqlStaticMethodsTranslator();

        var result = translator.InternTryTranslate(expression, out sqlMethodFound);

        return result;
    }
}
