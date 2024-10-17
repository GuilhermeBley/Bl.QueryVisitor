using Bl.QueryVisitor.MySql;
using Bl.QueryVisitor.MySql.Providers;
using Bl.QueryVisitor.MySql.Visitors;
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
    private readonly SelectVisitor _selectVisitor = new SelectVisitor();

    /// <summary>
    /// These items are used to replace the 'Property.Name', because it can improve by using index 
    /// </summary>
    private readonly IReadOnlyDictionary<string, string> _renamedProperties;
    private readonly ColumnNameProvider _columnNameProvider;

    public IItemTranslator ItemTranslator => _selectVisitor;
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

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
        _columnNameProvider = new QuotesColumnNameProvider(_renamedProperties);
    }

    public SimpleQueryTranslatorResult Translate(Expression expression)
    {
        _whereBuilder.Clear();
        _columns.Clear();
        _parameters.Clear();
        _skip = null;
        _take = null;

        var orderResult = new OrderByExpressionVisitor(_columnNameProvider).Translate(expression);

        this.Visit(orderResult.Others);

        return new SimpleQueryTranslatorResult(
            Parameters: _parameters,
            Columns: _columns,
            HavingSql: NormalizeHaving(),
            OrderBySql: NormalizeOrderBy(orderResult.OrderBy),
            LimitSql: NormalizeLimit());
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

            var whereTranslator = new WhereVisitor(parameters: _parameters, columnNameProvider: _columnNameProvider);

            _whereBuilder.Append(whereTranslator.TranslateWhere(lambda.Body));

            return node;
        }
        else if (node.Method.Name == "Select")
        {
            var selectVisitor = _selectVisitor;

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

        return node;
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Convert:
                this.Visit(u.Operand);
                break;
            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }
        return u;
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

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }
        return e;
    }

    private class QuotesColumnNameProvider : ColumnNameProvider
    {
        public QuotesColumnNameProvider(IReadOnlyDictionary<string, string> directColumns) : base(directColumns)
        {
        }

        /// <summary>
        /// This column transformation basically transforms the column "ID" to "`ID`".
        /// If the column is direct, the value will not be changed because values that contain a schema separator won't be mapped.
        /// </summary>
        protected override string TransformColumn(string column)
        {
            const char MYSQL_COLUMN_NAME_SEPARATOR = '`';
            const char MYSQL_SCHEMA_SEPARATOR = '.';

            if (column.Contains(MYSQL_COLUMN_NAME_SEPARATOR, StringComparison.OrdinalIgnoreCase))
            {
                return column;
            }

            if (column.Contains(MYSQL_SCHEMA_SEPARATOR, StringComparison.OrdinalIgnoreCase))
            {
                return column;
            }

            return string.Concat(MYSQL_COLUMN_NAME_SEPARATOR, column, MYSQL_COLUMN_NAME_SEPARATOR);
        }
    }
}
