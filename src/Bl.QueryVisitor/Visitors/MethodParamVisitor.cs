using System.Linq.Expressions;
using System.Text;

namespace Bl.QueryVisitor.Visitors;

internal class MethodParamVisitor
    : ExpressionVisitor
{
    private readonly ParamDictionary _parameters;
    private StringBuilder _builder = new();
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    public MethodParamVisitor(ParamDictionary parameters)
    {
        _parameters = parameters;
    }

    public string TranslateMethod(Expression? expression)
    {
        _builder.Clear();

        Visit(expression);

        return _builder.ToString();
    }
}
