using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

public class WhereClauseVisitor : ExpressionVisitor
{
    private readonly List<InternalClauseInfo> _whereClauses = new List<InternalClauseInfo>();

    public IEnumerable<WhereClause> GetWhereClauses(Expression expression)
    {
        _whereClauses.Clear();

        List<WhereClause> clauses = new();
        List<ClauseInfo> orClauses = new();

        Visit(expression);

        foreach (var clause in _whereClauses)
        {
            if (clause.IsNextOrOperator)
            {
                orClauses.Add(clause);
                continue;
            }

            var whereClause = new WhereClause(orClauses);

            orClauses.Clear();
            clauses.Add(whereClause);
        }

        return clauses;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name != "Where")
            return base.VisitMethodCall(node);

        if (node.Arguments.Count == 2 && node.Arguments[1] is UnaryExpression unaryExpression)
        {
            ExtractBinaryClauses(null!);
        }
        
        return base.VisitMethodCall(node);
    }

    private void ExtractBinaryClauses(BinaryExpression node)
    {
        var nextNode = node;

        do
        {
            InternalClauseInfo clause;

            switch (nextNode.NodeType)
            {
                case ExpressionType.OrElse:
                    clause = ExtractClauseInfo(nextNode.Left);
                    clause.IsNextOrOperator = true;
                    _whereClauses.Add(clause);
                    break;
                default:
                    clause = ExtractClauseInfo(nextNode.Left);
                    _whereClauses.Add(clause);
                    break;
            }

            nextNode = nextNode.Right.CanReduce ?
                nextNode.Right as BinaryExpression :
                null;

        } while (nextNode is BinaryExpression);
    }

    private static InternalClauseInfo ExtractClauseInfo(Expression node)
    {
        if (node is not BinaryExpression memberExpression)
            throw new ArgumentException();
        
        var propertyName = memberExpression.Left.ToString();
        var constant = memberExpression.Right as ConstantExpression;
        var value = constant?.Value;

        if (constant is null)
            throw new ArgumentException();
        
        var comparer = memberExpression.NodeType.ToString();
            
        return new InternalClauseInfo(propertyName, comparer, value);
    }

    private record InternalClauseInfo
        : ClauseInfo
    {
        public bool IsNextOrOperator = false;

        public InternalClauseInfo(string PropertyName, string ComparerType, object? Value)
            : base(PropertyName, ComparerType, Value)
        {
        }
    }
}

public class WhereClause
    : IReadOnlyList<ClauseInfo>
{
    private readonly IReadOnlyList<ClauseInfo> _whereClauses;
    public int Count => _whereClauses.Count;
    public readonly bool NextIsOrOperator;
    public ClauseInfo this[int index] => _whereClauses[index];

    internal WhereClause(IEnumerable<ClauseInfo> clauses)
    {
        _whereClauses = clauses.ToImmutableArray();

        if (_whereClauses.Count == 0)
            throw new IndexOutOfRangeException("It requires at least one item.");

        NextIsOrOperator = _whereClauses.Count > 1;
    }

    public IEnumerator<ClauseInfo> GetEnumerator()
    {
        return _whereClauses.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _whereClauses.GetEnumerator();
    }
}


public record ClauseInfo(
    string PropertyName,
    string ComparerType,
    object? Value
);