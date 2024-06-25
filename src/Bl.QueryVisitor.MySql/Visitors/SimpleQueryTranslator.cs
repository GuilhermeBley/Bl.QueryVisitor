using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;

namespace Bl.QueryVisitor.Visitors;

public interface ISimpleQueryTranslator
{
    SimpleQueryTranslatorResult Translate(Expression expression);
}

public class SimpleQueryTranslator
    : ExpressionVisitor,
    ISimpleQueryTranslator
{
    /// <summary>
    /// Storage all clauses, example: "(x > 1) AND (x < 10)".
    /// </summary>
    private readonly StringBuilder _whereBuilder = new();
    private uint? _skip = null;
    private uint? _take = null;
    private readonly ParamDictionary _parameters = new();
    private readonly List<string> _columns = new();

    /// <summary>
    /// These items are used to replace the 'Property.Name', because it can improve by using index 
    /// </summary>
    private readonly IReadOnlyDictionary<string, string> _renamedProperties;

    public SimpleQueryTranslator()
        : this(Enumerable.Empty<KeyValuePair<string, string>>())
    {
    }

    public SimpleQueryTranslator(IEnumerable<KeyValuePair<string, string>> renamedProperties)
        : this(renamedPropertiesDictionary: renamedProperties.ToDictionary(item => item.Key, item => item.Value))
    {
    }

    public SimpleQueryTranslator(IReadOnlyDictionary<string, string> renamedPropertiesDictionary)
    {
        _renamedProperties = renamedPropertiesDictionary.ToImmutableDictionary();
    }

    public SimpleQueryTranslatorResult Translate(Expression expression)
    {
        _whereBuilder.Clear();
        _columns.Clear();
        _parameters.Clear();
        _skip = null;
        _take = null;

        var orderResult = new OrderByExpressionVisitor(_renamedProperties).Translate(expression);

        this.Visit(orderResult.Others);

        return new SimpleQueryTranslatorResult(
            Parameters: _parameters,
            Columns: _columns,
            HavingSql: NormalizeHaving(),
            OrderBySql: NormalizeOrderBy(orderResult.OrderBy),
            LimitSql: NormalizeLimit());
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }
        return e;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
        {
            this.Visit(node.Arguments[0]);

            LambdaExpression lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);

            if (_whereBuilder.Length > 0)
            {
                _whereBuilder.Append(" AND ");
            }

            this.Visit(lambda.Body);

            return node;
        }
        else if (node.Method.Name == "Select")
        {
            var selectVisitor = new SelectVisitor();

            var stripedQuoteSelect = StripQuotes(node.Arguments[1]);

            var result = selectVisitor.TranslateColumns(stripedQuoteSelect);

            _columns.AddRange(result);

            Expression nextExpression = node.Arguments[0];

            return this.Visit(nextExpression);
        }
        else if (node.Method.Name == "Take")
        {
            if (this.ParseTakeExpression(node))
            {
                Expression nextExpression = node.Arguments[0];
                return this.Visit(nextExpression);
            }
        }
        else if (node.Method.Name == "Skip")
        {
            if (this.ParseSkipExpression(node))
            {
                Expression nextExpression = node.Arguments[0];
                return this.Visit(nextExpression);
            }
        }

        var methodVisitor = new MethodParamVisitor(_parameters, _renamedProperties);

        var sql = methodVisitor.TranslateMethod(node);

        _whereBuilder.Append(sql);

        return node;
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Not:
                _whereBuilder.Append(" NOT ");
                this.Visit(u.Operand);
                break;
            case ExpressionType.Convert:
                this.Visit(u.Operand);
                break;
            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }
        return u;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    protected override Expression VisitBinary(BinaryExpression b)
    {
        _whereBuilder.Append("(");
        this.Visit(b.Left);

        switch (b.NodeType)
        {
            case ExpressionType.And:
                _whereBuilder.Append(" AND ");
                break;

            case ExpressionType.AndAlso:
                _whereBuilder.Append(" AND ");
                break;

            case ExpressionType.Or:
                _whereBuilder.Append(" OR ");
                break;

            case ExpressionType.OrElse:
                _whereBuilder.Append(" OR ");
                break;

            case ExpressionType.Equal:
                if (IsNullConstant(b.Right))
                {
                    _whereBuilder.Append(" IS ");
                }
                else
                {
                    _whereBuilder.Append(" = ");
                }
                break;

            case ExpressionType.NotEqual:
                if (IsNullConstant(b.Right))
                {
                    _whereBuilder.Append(" IS NOT ");
                }
                else
                {
                    _whereBuilder.Append(" != ");
                }
                break;

            case ExpressionType.LessThan:
                _whereBuilder.Append(" < ");
                break;

            case ExpressionType.LessThanOrEqual:
                _whereBuilder.Append(" <= ");
                break;

            case ExpressionType.GreaterThan:
                _whereBuilder.Append(" > ");
                break;

            case ExpressionType.GreaterThanOrEqual:
                _whereBuilder.Append(" >= ");
                break;

            default:
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));

        }

        this.Visit(b.Right);
        _whereBuilder.Append(")");
        return b;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        var convertedExp = Expression.Convert(node, typeof(object));

        var instantiator = Expression
            .Lambda<Func<object>>(convertedExp)
            .Compile();
        var res = instantiator();

        return Visit(Expression.Constant(res));
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        IQueryable? q = c.Value as IQueryable;

        if (q == null && c.Value == null)
        {
            var parameter = _parameters.AddNextParam(null);

            _whereBuilder.Append(parameter);
        }
        else if (q == null)
        {
            ArgumentNullException.ThrowIfNull(c.Value);

            var parameter = _parameters.AddNextParam(c.Value);

            _whereBuilder.Append(parameter);
        }

        return c;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            var columnName = _renamedProperties
                .TryGetValue(m.Member.Name, out var renamedValue)
                    ? renamedValue
                    : m.Member.Name;

            _whereBuilder.Append(columnName);
            return m;
        }
        if (m.Expression is ConstantExpression constant)
        {
            var objectMember = Expression.Convert(m, typeof(object));

            var getterLambda = Expression.Lambda<Func<object>>(objectMember);

            var getter = getterLambda.Compile();

            var result = getter();

            return Visit(Expression.Constant(result));
        }

        if (m.NodeType == ExpressionType.MemberAccess &&
            SqlStaticMethodsTranslator.TryTranslate(m, out var sqlMethodFound))
        {
            _whereBuilder.Append(sqlMethodFound);

            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }

    protected bool IsNullConstant(Expression exp)
    {
        return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
    }

    private bool ParseTakeExpression(MethodCallExpression expression)
    {
        ConstantExpression sizeExpression = ConvertToConstant(expression.Arguments[1]);

        uint size;
        if (uint.TryParse(sizeExpression.Value?.ToString(), out size))
        {
            _take = size;
            return true;
        }

        return false;
    }

    private bool ParseSkipExpression(MethodCallExpression expression)
    {
        ConstantExpression sizeExpression = ConvertToConstant(expression.Arguments[1]);

        uint size;
        if (uint.TryParse(sizeExpression.Value?.ToString(), out size))
        {
            _skip = size;
            return true;
        }

        return false;
    }

    private string NormalizeHaving()
    {
        var whereClauses = _whereBuilder.ToString();

        if (string.IsNullOrWhiteSpace(whereClauses))
            return string.Empty;

        return string.Concat('\n', "HAVING ", whereClauses);
    }

    private string NormalizeLimit()
    {
        if (_skip is null && _take is null)
            return string.Empty;

        var take = _take ?? int.MaxValue;

        var skip = _skip ?? 0;

        if (skip == 0)
            return string.Concat('\n', "LIMIT ", take);

        return string.Concat('\n', "LIMIT ", take, " OFFSET ", skip);
    }

    private static string NormalizeOrderBy(string orders)
    {
        if (string.IsNullOrWhiteSpace(orders))
            return string.Empty;

        return string.Concat('\n', "ORDER BY ", orders);
    }

    private static ConstantExpression ConvertToConstant(Expression node)
    {
        if (node is ConstantExpression constValue)
        {
            return constValue;
        }

        var convertedExp = Expression.Convert(node, typeof(object));

        var instantiator = Expression
            .Lambda<Func<object>>(convertedExp)
            .Compile();
        var res = instantiator();

        return Expression.Constant(res);
    }
}
