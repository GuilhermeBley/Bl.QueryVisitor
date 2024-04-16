using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

internal class SelectVisitor : ExpressionVisitor
{
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

    protected override Expression VisitMember(MemberExpression node)
    {
        if (Type.GetTypeCode(node.Type) == TypeCode.Object)
            return base.VisitMember(node);

        _columns.Add(node.Member.Name);
        return base.VisitMember(node);
    }
}
