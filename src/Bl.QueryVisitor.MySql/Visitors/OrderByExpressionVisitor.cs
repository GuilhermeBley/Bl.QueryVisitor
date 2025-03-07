using Bl.QueryVisitor.MySql.Providers;
using Bl.QueryVisitor.MySql.Visitors;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Text;

namespace Bl.QueryVisitor.Visitors;

/// <summary>
/// Storage all orderers, example "Field ASC, Field2 DESC".
/// </summary>
internal class OrderByExpressionVisitor
    : ExpressionVisitor
{
    private readonly ColumnNameProvider _columnNameProvider;
    private readonly Type _modelType;
    private readonly List<OrderByItem> _orderItems = new List<OrderByItem>();

    public OrderByExpressionVisitor(
        ColumnNameProvider columnNameProvider,
        Type modelType)
    {
        _columnNameProvider = columnNameProvider;
        _modelType = modelType;
    }

    public Result Translate(Expression? node)
    {
        _orderItems.Clear();

        node = new MySqlNullSimplifier().Visit(node ?? throw new InvalidOperationException());

        var t = node.ToString();

        this.Visit(node); // visiting nodes to populate _orderItems

        var orderSqlCommand = string.Join(
            ", ",
            _orderItems.Select(e => e));

        return new Result(node, orderSqlCommand);
    }

    /// <summary>
    /// It returns a expression without the 'OrderBy', 'OrderByDescending', 'ThenBy' and 'ThenByDescending' method call expressions.
    /// </summary>
    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        if (m.Method.Name == "ThenBy")
        {
            if (CanParseOrderByExpression(m, out var member))
            {
                AddAndParseOrderByExpression(member, true);

                Expression nextExpression = m.Arguments[0];

                return this.Visit(nextExpression); // going to the next
            }
        }
        else if (m.Method.Name == "OrderBy")
        {
            if (CanParseOrderByExpression(m, out var member))
            {
                AddAndParseOrderByExpression(member, true);

                return m; // stopping node visit
            }
        }
        else if (m.Method.Name == "ThenByDescending")
        {
            if (CanParseOrderByExpression(m, out var member))
            {
                AddAndParseOrderByExpression(member, false);

                Expression nextExpression = m.Arguments[0];

                return this.Visit(nextExpression); // going to the next
            }
        }
        else if (m.Method.Name == "OrderByDescending")
        {
            if (CanParseOrderByExpression(m, out var member))
            {
                AddAndParseOrderByExpression(member, false);

                return m; // stopping node visit
            }
        }

        return base.VisitMethodCall(m);
    }

    private static bool CanParseOrderByExpression(MethodCallExpression expression, [NotNullWhen(true)] out MemberExpression? memberExpression)
    {
        UnaryExpression unary = (UnaryExpression)expression.Arguments[1];
        LambdaExpression lambdaExpression = (LambdaExpression)unary.Operand;

        memberExpression = lambdaExpression.Body as MemberExpression;

        return memberExpression is not null;
    }

    private void AddAndParseOrderByExpression(MemberExpression body, bool isAsc)
    {
        var items = _orderItems;

        if (body?.Member.DeclaringType == _modelType)
        {
            var columnName = _columnNameProvider.GetColumnName(body.Member.Name);

            OrderByItem newOrder = new(columnName, isAsc, body.Member.Name);

            var itemAlreadyAdded = items.FirstOrDefault(e => OrderByItem.Comparer.Equals(newOrder, e));

            if (itemAlreadyAdded is not null)
            {
                _orderItems.Remove(itemAlreadyAdded);
            }

            items.Insert(0, newOrder);
        }
    }

    private void ParseOrderByExpressionToBuilder(MethodCallExpression expression, bool isAsc, bool reorder, StringBuilder builder)
    {
        var order = isAsc ? "ASC" : "DESC";
        
        UnaryExpression unary = (UnaryExpression)expression.Arguments[1];
        LambdaExpression lambdaExpression = (LambdaExpression)unary.Operand;

        MemberExpression? body = lambdaExpression.Body as MemberExpression;
        if (body?.Member.DeclaringType == _modelType)
        {
            string newOrder;

            var columnName = _columnNameProvider.GetColumnName(body.Member.Name);

            if (builder.Length == 0)
                newOrder = string.Format("{0} {1}", columnName, order);
            else if (reorder)
                newOrder = string.Format("{0} {1}, {2}", columnName, order, builder.ToString());
            else
                newOrder = string.Format("{0}, {1} {2}", builder.ToString(), columnName, order);

            builder.Clear();

            builder.Append(newOrder);
        }
    }

    private class OrderByItem
    {
        public readonly static IEqualityComparer<OrderByItem?> Comparer = new InternalComparer();

        /// <summary>
        /// Class property name
        /// </summary>
        public string PropertyName { get; }
        /// <summary>
        /// The real SQL value
        /// </summary>
        public string SqlValue { get; }
        public bool Asc { get; }

        public OrderByItem(string sqlValue, bool asc, string propertyName)
        {
            SqlValue = sqlValue;
            Asc = asc;
            PropertyName = propertyName;
        }

        public override string ToString()
        {
            return string.Concat(SqlValue, ' ', Asc ? "ASC" : "DESC");
        }

        private class InternalComparer : IEqualityComparer<OrderByItem?>
        {
            public bool Equals(OrderByItem? x, OrderByItem? y)
            {
                if (x == null || y == null) return false;

                return x.PropertyName.Equals(y.PropertyName, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode([DisallowNull] OrderByItem? obj)
            {
                return obj.PropertyName.GetHashCode(StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public record Result(
        Expression Others,
        string OrderBy);
}
