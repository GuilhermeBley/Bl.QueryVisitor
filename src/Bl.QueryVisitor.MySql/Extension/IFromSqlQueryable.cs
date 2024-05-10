using System.Linq.Expressions;

namespace Bl.QueryVisitor.Extension;

public interface IFromSqlQueryable<TEntity>
    : IQueryable<TEntity>,
    IOrderedQueryable<TEntity>,
    IAsyncEnumerable<TEntity>,
    IFromSqlTextQuery
{
    new IFromSqlQueryProvider Provider { get; }
}

public interface IFromSqlQueryProvider
    : IQueryProvider
{
    TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
}

public interface IFromSqlTextQuery
{
    string ToSqlText();
}