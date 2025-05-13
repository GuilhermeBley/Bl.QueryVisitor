using Bl.QueryVisitor.MySql.BlExpressions;
using Bl.QueryVisitor.MySql.Providers;
using Bl.QueryVisitor.Visitors;
using System.Linq.Expressions;
using System.Text;

namespace Bl.QueryVisitor.MySql.Visitors;

internal class SqlMethodSimplifier : ExpressionVisitor
{
    private readonly ParamDictionary _parameters;
    private readonly StringBuilder _builder;
    private readonly ColumnNameProvider _columnNameProvider;
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    public SqlMethodSimplifier(ParamDictionary parameters, ColumnNameProvider columnNameProvider)
    {
        _builder = new StringBuilder();
        _parameters = parameters;
        _columnNameProvider = columnNameProvider;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == "Equals" && node.Method.IsStatic)
        {
            base.Visit(node.Arguments[0]);

            _builder.Append(" = ");

            base.Visit(node.Arguments[1]);

            return CreateSqlExpressionByCurrentBuilder(node)
                ?? throw new InvalidOperationException($"Failed to cast node {node}.");
        }
        if (node.Method.Name == "Equals")
        {
            _builder.Append('(');

            base.Visit(node.Object);

            _builder.Append(" = ");

            base.Visit(node.Arguments[0]);

            _builder.Append(')');

            return CreateSqlExpressionByCurrentBuilder(node)
                ?? throw new InvalidOperationException($"Failed to cast node {node}.");
        }

        if (node.Method.Name == "ToString" && !node.Method.IsStatic)
        {
            var convertedExp = Expression.Convert(node, typeof(object));

            var instantiator = Expression
                .Lambda<Func<object>>(convertedExp)
                .Compile();
            var res = instantiator();

            return this.VisitConstant(Expression.Constant(res));
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

            return CreateSqlExpressionByCurrentBuilder(node)
                ?? throw new InvalidOperationException($"Failed to cast node {node}.");
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

            return CreateSqlExpressionByCurrentBuilder(node)
                ?? throw new InvalidOperationException($"Failed to cast node {node}.");
        }

        if (node.Method.Name == "Contains" && node.Arguments.Count == 1)
        {
            _builder.Append('(');

            base.Visit(node.Object);

            _builder.Append(" LIKE ");

            _builder.Append("CONCAT('%',");
            base.Visit(node.Arguments[0]);
            _builder.Append(",'%')");

            _builder.Append(')');

            return CreateSqlExpressionByCurrentBuilder(node)
                ?? throw new InvalidOperationException($"Failed to cast node {node}.");
        }

        if (node.Method.Name == "Contains" && node.Arguments.Count == 2)
        {
            if (node.Arguments[1] is not MemberExpression columnNameExp)
                throw new InvalidOperationException($"Column name was not found in expression {node}.");

            var arrayValuesDelegate =
                Expression.Lambda(node.Arguments[0]).Compile();

            var arrayValues = arrayValuesDelegate.DynamicInvoke()
                as System.Collections.IEnumerable
                ?? Enumerable.Empty<object>();

            List<object> inArguments = new();
            foreach (var arg in arrayValues)
            {
                inArguments.Add(arg);
            }

            if (!inArguments.Any())
            {
                //
                // Empty array, it's not possible to use 'IN' with an empty array.
                //
                return node;
            }

            // column name
            _builder.Append(
                _columnNameProvider.GetColumnName(columnNameExp.Member.Name));

            _builder.Append(" IN (");

            bool paramsStarted = false;
            foreach (var arg in inArguments)
            {
                if (paramsStarted)
                    _builder.Append(',');
                VisitConstant(Expression.Constant(arg));
                paramsStarted = true;
            }

            _builder.Append(')');

            return CreateSqlExpressionByCurrentBuilder(node)
                ?? throw new InvalidOperationException($"Failed to cast node {node}.");
        }

        if (node.Method.Name == "StartsWith")
        {
            _builder.Append('(');

            base.Visit(node.Object);

            _builder.Append(" LIKE ");

            _builder.Append("CONCAT(");
            base.Visit(node.Arguments[0]);
            _builder.Append(",'%')");

            _builder.Append(')');

            return CreateSqlExpressionByCurrentBuilder(node)
                ?? throw new InvalidOperationException($"Failed to cast node {node}.");
        }

        if (!SqlStaticMethodsTranslator.TryTranslate(node, out var sqlMethod))
            return base.VisitMethodCall(node); // continue...

        _builder.Append(sqlMethod);

        return CreateSqlExpressionByCurrentBuilder(node)
            ?? throw new InvalidOperationException($"Failed to cast node {node}.");
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
            var columnName = _columnNameProvider.GetColumnName(m.Member.Name);

            _builder.Append(columnName);
            return m;
        }
        if (m.Expression is ConstantExpression constant)
        {
            var convertedExp = Expression.Convert(m, typeof(object));

            var instantiator = Expression
                .Lambda<Func<object>>(convertedExp)
                .Compile();
            var res = instantiator();

            return this.VisitConstant(Expression.Constant(res));
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }

    protected override Expression VisitExtension(Expression node)
    {
        if (node is SqlCommandExpression) return node;

        return base.VisitExtension(node);
    }

    private Expression? CreateSqlExpressionByCurrentBuilder(MethodCallExpression node)
    {
        var sql = _builder.ToString();
        _builder.Clear();
        if (string.IsNullOrWhiteSpace(sql))
            return null;
        return VisitExtension(new SqlCommandExpression(sql, node));
    }
}
