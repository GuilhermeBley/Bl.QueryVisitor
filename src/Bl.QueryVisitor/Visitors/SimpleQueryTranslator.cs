using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

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
    /// <summary>
    /// Storage all orderers, example "Field ASC, Field2 DESC".
    /// </summary>
    private string _orderBy = string.Empty;
    private uint? _skip = null;
    private uint? _take = null;
    private readonly Dictionary<string, object?> _parameters = new();
    private readonly List<string> _columns = new();
    private int _lastParamId = 1000;

    public SimpleQueryTranslator()
    {
    }

    public SimpleQueryTranslatorResult Translate(Expression expression)
    {
        _whereBuilder.Clear();
        _columns.Clear();
        _orderBy = string.Empty;
        _lastParamId = 1000;
        _parameters.Clear();
        _skip = null;
        _take = null;
        
        this.Visit(expression);

        return new SimpleQueryTranslatorResult(
            Parameters: _parameters,
            Columns: _columns,
            HavingSql: NormalizeHaving(),
            OrderBySql: NormalizeOrderBy(),
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

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
        {
            this.Visit(m.Arguments[0]);
            LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
            this.Visit(lambda.Body);
            return m;
        }
        else if (m.Method.Name == "Select")
        {
            var selectVisitor = new SelectVisitor();

            var stripedQuoteSelect = StripQuotes(m.Arguments[1]);

            var result = selectVisitor.TranslateColumns(stripedQuoteSelect);

            _columns.AddRange(result);

            Expression nextExpression = m.Arguments[0];

            return this.Visit(nextExpression);
        }
        else if (m.Method.Name == "Take")
        {
            if (this.ParseTakeExpression(m))
            {
                Expression nextExpression = m.Arguments[0];
                return this.Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "Skip")
        {
            if (this.ParseSkipExpression(m))
            {
                Expression nextExpression = m.Arguments[0];
                return this.Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "OrderBy" || m.Method.Name == "ThenBy")
        {
            if (this.ParseOrderByExpression(m, "ASC"))
            {
                Expression nextExpression = m.Arguments[0];
                return this.Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "OrderByDescending" || m.Method.Name == "ThenByDescending")
        {
            if (this.ParseOrderByExpression(m, "DESC"))
            {
                Expression nextExpression = m.Arguments[0];
                return this.Visit(nextExpression);
            }
        }

        throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
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

        var lastParamIdText = $"@P{_lastParamId}";

        if (q == null && c.Value == null)
        {
            _whereBuilder.Append(lastParamIdText);
            _parameters.Add(lastParamIdText, null);
        }
        else if (q == null)
        {
            ArgumentNullException.ThrowIfNull(c.Value);

            _whereBuilder.Append(lastParamIdText);
            _parameters.Add(lastParamIdText, c.Value);
        }

        _lastParamId++; // always go to the next param

        return c;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            _whereBuilder.Append(m.Member.Name);
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

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }

    protected bool IsNullConstant(Expression exp)
    {
        return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
    }

    private bool ParseOrderByExpression(MethodCallExpression expression, string order)
    {
        UnaryExpression unary = (UnaryExpression)expression.Arguments[1];
        LambdaExpression lambdaExpression = (LambdaExpression)unary.Operand;

        MemberExpression? body = lambdaExpression.Body as MemberExpression;
        if (body != null)
        {
            if (string.IsNullOrEmpty(_orderBy))
            {
                _orderBy = string.Format("{0} {1}", body.Member.Name, order);
            }
            else
            {
                _orderBy = string.Format("{0} {1}, {2}", body.Member.Name, order, _orderBy);
            }

            return true;
        }

        return false;
    }

    private bool ParseTakeExpression(MethodCallExpression expression)
    {
        ConstantExpression sizeExpression;
        
        if (expression.Arguments[1] is ConstantExpression)
            sizeExpression = (ConstantExpression)expression.Arguments[1];
        else
            sizeExpression = ConvertToConstant(expression.Arguments[1]);

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
        ConstantExpression sizeExpression = (ConstantExpression)expression.Arguments[1];

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

    private string NormalizeOrderBy()
    {
        if (string.IsNullOrWhiteSpace(_orderBy))
            return string.Empty;

        return string.Concat('\n', "ORDER BY ", _orderBy);
    }

    private static ConstantExpression ConvertToConstant(Expression node)
    {
        var convertedExp = Expression.Convert(node, typeof(object));

        var instantiator = Expression
            .Lambda<Func<object>>(convertedExp)
            .Compile();
        var res = instantiator();

        return Expression.Constant(res);
    }
}
