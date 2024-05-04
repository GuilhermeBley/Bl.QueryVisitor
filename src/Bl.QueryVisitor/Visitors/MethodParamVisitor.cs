using System.Linq.Expressions;
using System.Text;

namespace Bl.QueryVisitor.Visitors;

internal class MethodParamVisitor
    : ExpressionVisitor
{
    private readonly ParamDictionary _parameters;
    private StringBuilder _builder = new();

    /// <summary>
    /// These items are used to replace the 'Property.Name', because it can improve by using index 
    /// </summary>
    private readonly IReadOnlyDictionary<string, string> _renamedProperties;
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    public MethodParamVisitor(ParamDictionary parameters, IReadOnlyDictionary<string, string> renamedProperties)
    {
        _parameters = parameters;
        _renamedProperties = renamedProperties;
    }

    public string TranslateMethod(Expression? expression)
    {
        _builder.Clear();

        Visit(expression);

        return _builder.ToString();
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == "Equals" && node.Method.IsStatic)
        {
            base.Visit(node.Arguments[0]);

            _builder.Append(" = ");

            base.Visit(node.Arguments[1]);

            return node;
        }
        if (node.Method.Name == "Equals")
        {
            base.Visit(node.Object);

            _builder.Append(" = ");

            base.Visit(node.Arguments[0]);

            return node;
        }


        if (node.Method.Name == "Concat" && node.Method.IsStatic)
        {
            _builder.Append("CONCAT(");

            var containsAfter = false;

            foreach (var arg in node.Arguments)
            {
                if (containsAfter)
                    _builder.Append(',');

                base.Visit(arg);

                containsAfter = true;
            }

            _builder.Append(')');

            return node;
        }
        if (node.Method.Name == "Concat")
        {
            _builder.Append("CONCAT(");

            base.Visit(node.Object);
            var containsAfter = false;

            foreach (var arg in node.Arguments)
            {
                if (containsAfter)
                    _builder.Append(',');
                
                base.Visit(arg);

                containsAfter = true;
            }

            _builder.Append(')');

            return node;
        }

        if (node.Method.Name == "Contains")
        {
            base.Visit(node.Object);

            _builder.Append(" LIKE ");

            _builder.Append("CONCAT('%',");
            base.Visit(node.Arguments[0]);
            _builder.Append(",'%') ");

            return node;
        }

        
        throw new NotSupportedException(string.Format("The method '{0}' is not supported", node.Method.Name));
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

            _builder.Append(parameter);
        }
        else if (q == null)
        {
            ArgumentNullException.ThrowIfNull(c.Value);

            var parameter = _parameters.AddNextParam(c.Value);

            _builder.Append(parameter);
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

            _builder.Append(columnName);
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
}
