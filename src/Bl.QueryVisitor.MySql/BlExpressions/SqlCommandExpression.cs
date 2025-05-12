using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.BlExpressions;

public class SqlCommandExpression : Expression
{
    private string _command;

    public override bool CanReduce => true;
    public override ExpressionType NodeType => ExpressionType.Constant;
    public string Command => _command;

    public SqlCommandExpression(string command)
    {
        _command = command;
    }

    public override Expression Reduce()
    {
        return Expression.Constant(_command, typeof(string));
    }
}
