using Bl.QueryVisitor.Visitors;
using System.Linq.Expressions;
using System.Text;

namespace Bl.QueryVisitor.MySql.Visitors;

internal class WhereVisitor
    : ExpressionVisitor
{
    /// <summary>
    /// Storage all clauses, example: "(x > 1) AND (x < 10)".
    /// </summary>
    private readonly StringBuilder _whereBuilder = new();
    private readonly ParamDictionary _parameters;
    /// <summary>
    /// These items are used to replace the 'Property.Name', because it can improve by using index 
    /// </summary>
    private readonly IReadOnlyDictionary<string, string> _renamedProperties;
    public WhereVisitor(
        ParamDictionary parameters,
        IReadOnlyDictionary<string, string> renamedProperties)
    {
        _parameters = parameters;
        _renamedProperties = renamedProperties;
    }

    public string TranslateWhere(Expression expression)
    {
        Visit(expression);

        return _whereBuilder.ToString();
    }
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
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

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        _whereBuilder.Append("IF(");
        Visit(StripQuotes(node.Test));
        _whereBuilder.Append(',');
        Visit(StripQuotes(node.IfFalse));
        _whereBuilder.Append(',');
        Visit(StripQuotes(node.IfTrue));
        _whereBuilder.Append(")");

        return node;
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }
        return e;
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

        if (q is null && c.Value is null)
        {
            //
            // Null values can't be expressed in variables
            //
            _whereBuilder.Append("NULL");
        }
        else if (q is null)
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

        if (m.NodeType == ExpressionType.MemberAccess &&
            SqlMethodParameterTranslator.TryTranslate(m, _renamedProperties, out var sqlFunctionFound))
        {
            _whereBuilder.Append(sqlFunctionFound);

            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }

    protected bool IsNullConstant(Expression exp)
    {
        return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
    }
}
