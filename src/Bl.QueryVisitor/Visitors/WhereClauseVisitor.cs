using System.Collections;
using System.Collections.Immutable;
using System.IO.Pipes;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

public class WhereClauseVisitor : ExpressionVisitor
{
    private IEnumerable<WhereClause> _clauseInfos
        = Enumerable.Empty<WhereClause>();

    public IEnumerable<WhereClause> GetWhereClauses(Expression expression)
    {
        Visit(expression);

        return _clauseInfos;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name != "Where")
            return base.VisitMethodCall(node);

        var whereVisitor = new InternalBinaryWhereClauseVisitor();

        _clauseInfos = whereVisitor.GetWhereClauses(node);

        return base.VisitMethodCall(node);
    }

    private record InternalClauseInfo
        : ClauseInfo
    {
        public bool IsNextAndOperator = false;

        public InternalClauseInfo(string PropertyName, string ComparerType, object? Value)
            : base(PropertyName, ComparerType, Value)
        {
        }
    }

    private class InternalBinaryWhereClauseVisitor 
        : ExpressionVisitor
    {
        private readonly List<InternalClauseInfo> _whereClauses = new List<InternalClauseInfo>();

        public IEnumerable<WhereClause> GetWhereClauses(Expression expression)
        {
            _whereClauses.Clear();

            List<WhereClause> clauses = new();
            List<ClauseInfo> andClauses = new();

            Visit(expression);

            foreach (var clause in _whereClauses)
            {
                if (clause.IsNextAndOperator)
                {
                    andClauses.Add(clause);
                    continue;
                }

                var whereClause = andClauses.Any() ?
                    new WhereClause(andClauses) :
                    new WhereClause(clause);

                andClauses.Clear();
                clauses.Add(whereClause);
            }

            return clauses;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (_whereClauses.Any())
                return base.VisitBinary(node); // Already collected

            var nextNode = node;

            do
            {
                var binaryExp = nextNode.Left as BinaryExpression
                    ?? nextNode;

                if (binaryExp is null)
                    break;

                InternalClauseInfo clause;

                switch (nextNode.NodeType)
                {
                    case ExpressionType.AndAlso:
                        clause = ExtractClauseInfo(binaryExp);
                        clause.IsNextAndOperator = true;
                        _whereClauses.Add(clause);
                        break;
                    default:
                        clause = ExtractClauseInfo(binaryExp);
                        _whereClauses.Add(clause);
                        break;
                }
                
                nextNode = nextNode.Right as BinaryExpression;

            } while (nextNode is BinaryExpression);

            return base.VisitBinary(node);
        }

        private static InternalClauseInfo ExtractClauseInfo(BinaryExpression node)
        {
            var propertyName = node.Left.ToString();
            var constant = node.Right as ConstantExpression;
            var value = constant?.Value;

            if (constant is null)
                throw new ArgumentException();

            var comparer = node.NodeType.ToString();

            return new InternalClauseInfo(propertyName, comparer, value);
        }
    }
}

public class WhereClause
    : IReadOnlyList<ClauseInfo>
{
    private readonly IReadOnlyList<ClauseInfo> _whereClauses;
    public int Count => _whereClauses.Count;
    public readonly bool AndOperator;
    public ClauseInfo this[int index] => _whereClauses[index];

    internal WhereClause(ClauseInfo clause)
        : this(new[] { clause })
    {

    }

    internal WhereClause(IEnumerable<ClauseInfo> clauses)
    {
        _whereClauses = clauses.ToImmutableArray();

        if (_whereClauses.Count == 0)
            throw new IndexOutOfRangeException("It requires at least one item.");

        AndOperator = _whereClauses.Count > 1;
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