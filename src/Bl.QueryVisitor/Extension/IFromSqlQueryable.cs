using Microsoft.EntityFrameworkCore.Query;

namespace Bl.QueryVisitor.Extension;

public interface IFromSqlQueryable<TEntity>
    : IQueryable<TEntity>,
    IQueryingEnumerable,
    IOrderedQueryable<TEntity>
{
    new IAsyncQueryProvider Provider { get; }
}

public interface IFromSqlQueryProvider
    : IAsyncQueryProvider
{
}
