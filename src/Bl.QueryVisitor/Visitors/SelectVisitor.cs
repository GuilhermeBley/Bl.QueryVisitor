using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

internal class SelectVisitor : ExpressionVisitor
{
    private readonly List<string> _columns = new List<string>();

    public IEnumerable<string> TranslateColumns(Expression expression)
    {
        _columns.Clear();
        Visit(expression);
        return _columns.ToArray();
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        _columns.Add(node.Member.Name);
        return base.VisitMember(node);
    }
}
