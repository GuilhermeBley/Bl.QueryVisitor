using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.BlExpressions;

public class SqlCommandExpression : BlExpression
{
    private string _command;
    private MethodCallExpression _callExpression;

    public override bool CanReduce => true;
    public override ExpressionType NodeType => ExpressionType.Extension;
    public string Command => _command;
    public override Type Type => _callExpression.Type;

    public SqlCommandExpression(string command, MethodCallExpression callExpression)
    {
        _command = command;
        _callExpression = callExpression;
    }

    public override string ToString()
    {
        return _command;
    }

    public override Expression Reduce()
    {
        return _callExpression;
    }
}
