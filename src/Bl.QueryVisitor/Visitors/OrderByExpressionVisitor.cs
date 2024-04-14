using System.Linq.Expressions;
using System.Text;

namespace Bl.QueryVisitor.Visitors;

/// <summary>
/// Storage all orderers, example "Field ASC, Field2 DESC".
/// </summary>
internal class OrderByExpressionVisitor
    : ExpressionVisitor
{
    /// <summary>
    /// These items are used to replace the 'Property.Name', because it can improve by using index 
    /// </summary>
    private readonly IReadOnlyDictionary<string, string> _renamedProperties;

    public OrderByExpressionVisitor(IReadOnlyDictionary<string, string> renamedProperties)
    {
        _renamedProperties = renamedProperties;
    }

    private readonly List<MethodCallExpression> _orderCalls = new();

    public Result Translate(Expression? node)
    {
        _orderCalls.Clear();

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

        List<StringBuilder> orders = new();
        StringBuilder? currentOrder = null;

        foreach (var m in correctOrderByReading)
            switch (m.Method.Name)
            {
                case ("OrderBy"):
                    currentOrder = new();
                    orders.Insert(0, currentOrder);
                    ParseOrderByExpressionToBuilder(m, isAsc: true, reorder: true, currentOrder);
                    break;
                case ("OrderByDescending"):
                    currentOrder = new();
                    orders.Insert(0, currentOrder);
                    ParseOrderByExpressionToBuilder(m, isAsc: false, reorder: true, currentOrder);
                    break;
                case ("ThenBy"):
                    if (currentOrder is null)
                        throw new InvalidOperationException("Before use 'ThenBy', add 'OrderBy' or 'OrderbyDescending'.");
                    ParseOrderByExpressionToBuilder(m, isAsc: true, reorder: false, currentOrder);
                    break;
                case ("ThenByDescending"):
                    if (currentOrder is null)
                        throw new InvalidOperationException("Before use 'ThenByDescending', add 'OrderBy' or 'OrderbyDescending'.");
                    ParseOrderByExpressionToBuilder(m, isAsc: false, reorder: false, currentOrder);
                    break;
                default:
                    throw new NotImplementedException("Method '' not mapped.");
            }

        return string.Join(", ", orders);
    }

    private void ParseOrderByExpressionToBuilder(MethodCallExpression expression, bool isAsc, bool reorder, StringBuilder builder)
    {
        var order = isAsc ? "ASC" : "DESC";

        UnaryExpression unary = (UnaryExpression)expression.Arguments[1];
        LambdaExpression lambdaExpression = (LambdaExpression)unary.Operand;

        MemberExpression? body = lambdaExpression.Body as MemberExpression;
        if (body != null)
        {
            var newOrder = string.Empty;

            var columnName = _renamedProperties
               .TryGetValue(body.Member.Name, out var renamedValue)
                   ? renamedValue
                   : body.Member.Name;

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

    public record Result(
        Expression Others,
        string OrderBy);
}
