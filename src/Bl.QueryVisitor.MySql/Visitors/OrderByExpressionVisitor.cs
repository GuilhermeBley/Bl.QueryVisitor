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

    public OrderByExpressionVisitor(
        ColumnNameProvider columnNameProvider,
        Type modelType)
    {
        _columnNameProvider = columnNameProvider;
        _modelType = modelType;
    }

    private readonly List<MethodCallExpression> _orderCalls = new();

    public Result Translate(Expression? node)
    {
        _orderCalls.Clear();

        node = new MySqlNullSimplifier().Visit(node);

        var editedExpression = this.Visit(node) ?? throw new InvalidOperationException();

        var orderText = ParseOrderByExpression();

        return new Result(editedExpression, orderText);
    }

    /// <summary>
    /// It returns a expression without the 'OrderBy', 'OrderByDescending', 'ThenBy' and 'ThenByDescending' method call expressions.
    /// </summary>
    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        if (m.Method.Name == "ThenBy")
        {
            if (CanParseOrderByExpression(m))
            {
                Expression nextExpression = m.Arguments[0];

                _orderCalls.Add(m);

                return this.Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "OrderBy")
        {
            if (CanParseOrderByExpression(m))
            {
                Expression nextExpression = m.Arguments[0];

                _orderCalls.Add(m);

                return this.Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "ThenByDescending")
        {
            if (CanParseOrderByExpression(m))
            {
                Expression nextExpression = m.Arguments[0];

                _orderCalls.Add(m);

                return this.Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "OrderByDescending")
        {
            if (CanParseOrderByExpression(m))
            {
                Expression nextExpression = m.Arguments[0];

                _orderCalls.Add(m);

                return this.Visit(nextExpression);
            }
        }

        return base.VisitMethodCall(m);
    }

    private static bool CanParseOrderByExpression(MethodCallExpression expression)
    {
        UnaryExpression unary = (UnaryExpression)expression.Arguments[1];
        LambdaExpression lambdaExpression = (LambdaExpression)unary.Operand;

        return lambdaExpression.Body is MemberExpression;
    }

    private string ParseOrderByExpression()
    {
        var correctOrderByReading = _orderCalls.Reverse<MethodCallExpression>();

        var orderItems = new List<OrderByItem>();
        
        foreach (var m in correctOrderByReading)
            switch (m.Method.Name)
            {
                case ("OrderBy"):
                    ParseOrderByExpressionToList(m, isAsc: true, reorder: true, orderItems);
                    break;
                case ("OrderByDescending"):
                    ParseOrderByExpressionToList(m, isAsc: false, reorder: true, orderItems);
                    break;
                case ("ThenBy"):
                    if (orderItems.Count == 0)
                        throw new InvalidOperationException("Before use 'ThenBy', add 'OrderBy' or 'OrderbyDescending'.");
                    ParseOrderByExpressionToList(m, isAsc: true, reorder: false, orderItems);
                    break;
                case ("ThenByDescending"):
                    if (orderItems.Count == 0)
                        throw new InvalidOperationException("Before use 'ThenByDescending', add 'OrderBy' or 'OrderbyDescending'.");
                    ParseOrderByExpressionToList(m, isAsc: false, reorder: false, orderItems);
                    break;
                default:
                    throw new NotImplementedException($"Method '{m.Method.Name}' not mapped.");
            }

        var orderSqlCommand = string.Join(
            ", ",
            orderItems.Select(e => e));

        return orderSqlCommand;
    }

    private void ParseOrderByExpressionToList(MethodCallExpression expression, bool isAsc, bool reorder, List<OrderByItem> items)
    {
        UnaryExpression unary = (UnaryExpression)expression.Arguments[1];
        LambdaExpression lambdaExpression = (LambdaExpression)unary.Operand;

        MemberExpression? body = lambdaExpression.Body as MemberExpression;
        if (body?.Member.DeclaringType == _modelType)
        {
            var columnName = _columnNameProvider.GetColumnName(body.Member.Name);

            OrderByItem newOrder = new(columnName, isAsc, body.Member.Name);

            if (items.Contains(newOrder, OrderByItem.Comparer))
            {
                return;
            }

            if (reorder)
                items.Clear();

            items.Add(newOrder);
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
