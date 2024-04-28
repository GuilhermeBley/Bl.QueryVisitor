using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

internal class MethodParamVisitor
    : ExpressionVisitor
{
    private readonly ParamDictionary _parameters;

    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    public MethodParamVisitor(ParamDictionary parameters)
    {
        _parameters = parameters;
    }

    
}
