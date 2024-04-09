using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Bl.QueryVisitor.Visitors;

public class SimpleQueryTranslator : ExpressionVisitor
{
    private StringBuilder sb = null!;
    private string _orderBy = string.Empty;
    private int? _skip = null;
    private int? _take = null;
    private string _whereClause = string.Empty;
    private readonly Dictionary<string, object?> _parameters = new();
    private int _lastParamId = 1000;

    private IReadOnlyDictionary<string, object?> Parameters => _parameters;
    public int? Skip
    {
        get
        {
            return _skip;
        }
    }

    public int? Take
    {
        get
        {
            return _take;
        }
    }

    public string OrderBy
    {
        get
        {
            return _orderBy;
        }
    }

    public string WhereClause
    {
        get
        {
            return _whereClause;
        }
    }

    public SimpleQueryTranslator()
    {
    }


    public string Translate(Expression expression)
    {
        this.sb = new StringBuilder();
        _lastParamId = 1000;
        _parameters.Clear();
        this.Visit(expression);
        _whereClause = this.sb.ToString();
        return _whereClause;
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
        else if (m.Method.Name == "OrderBy")
        {
            if (this.ParseOrderByExpression(m, "ASC"))
            {
                Expression nextExpression = m.Arguments[0];
                return this.Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "OrderByDescending")
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
                sb.Append(" NOT ");
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
        sb.Append("(");
        this.Visit(b.Left);

        switch (b.NodeType)
        {
            case ExpressionType.And:
                sb.Append(" AND ");
                break;

            case ExpressionType.AndAlso:
                sb.Append(" AND ");
                break;

            case ExpressionType.Or:
                sb.Append(" OR ");
                break;

            case ExpressionType.OrElse:
                sb.Append(" OR ");
                break;

            case ExpressionType.Equal:
                if (IsNullConstant(b.Right))
                {
                    sb.Append(" IS ");
                }
                else
                {
                    sb.Append(" = ");
                }
                break;

            case ExpressionType.NotEqual:
                if (IsNullConstant(b.Right))
                {
                    sb.Append(" IS NOT ");
                }
                else
                {
                    sb.Append(" <> ");
                }
                break;

            case ExpressionType.LessThan:
                sb.Append(" < ");
                break;

            case ExpressionType.LessThanOrEqual:
                sb.Append(" <= ");
                break;

            case ExpressionType.GreaterThan:
                sb.Append(" > ");
                break;

            case ExpressionType.GreaterThanOrEqual:
                sb.Append(" >= ");
                break;

            default:
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));

        }

        this.Visit(b.Right);
        sb.Append(")");
        return b;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        IQueryable? q = c.Value as IQueryable;

        var lastParamIdText = $"@P{_lastParamId}";

        if (q == null && c.Value == null)
        {
            sb.Append(lastParamIdText);
            _parameters.Add(lastParamIdText, null);
        }
        else if (q == null)
        {
            ArgumentNullException.ThrowIfNull(c.Value);

            sb.Append(lastParamIdText);
            _parameters.Add(lastParamIdText, c.Value);
        }

        _lastParamId++; // always go to the next param

        return c;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            sb.Append(m.Member.Name);
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
                _orderBy = string.Format("{0}, {1} {2}", _orderBy, body.Member.Name, order);
            }

            return true;
        }

        return false;
    }

    private bool ParseTakeExpression(MethodCallExpression expression)
    {
        ConstantExpression sizeExpression = (ConstantExpression)expression.Arguments[1];

        int size;
        if (int.TryParse(sizeExpression.Value?.ToString(), out size))
        {
            _take = size;
            return true;
        }

        return false;
    }

    private bool ParseSkipExpression(MethodCallExpression expression)
    {
        ConstantExpression sizeExpression = (ConstantExpression)expression.Arguments[1];

        int size;
        if (int.TryParse(sizeExpression.Value?.ToString(), out size))
        {
            _skip = size;
            return true;
        }

        return false;
    }
}
