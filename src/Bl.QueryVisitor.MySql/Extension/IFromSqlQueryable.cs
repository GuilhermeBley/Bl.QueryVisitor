using System.Linq.Expressions;

namespace Bl.QueryVisitor.Extension;

public interface IFromSqlQueryable<TEntity>
    : IQueryable<TEntity>,
    IOrderedQueryable<TEntity>,
    IAsyncEnumerable<TEntity>,
    IFromSqlTextQuery,
    ISqlModel
{
    new IFromSqlQueryProvider Provider { get; }
}

public interface IFromSqlQueryProvider
    : IQueryProvider
{
    TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
    Bl.QueryVisitor.Visitors.SimpleQueryTranslator GenerateTranslator();
}

public interface IFromSqlTextQuery
{
    string ToSqlText();
}

public interface ISqlModel
{
    Type ModelType { get; }
}