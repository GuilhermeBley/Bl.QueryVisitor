using Bl.QueryVisitor.Extension;

namespace Bl.QueryVisitor.MySql.Extension;
public static class CountExtension
{
    /// <summary>
    /// This method will give all the records count of the query.
    /// To improve the performance, ensure to map all columns with 'SetColumnName' and then use in the queryable the method 'EnsureAllColumnSet'.
    /// </summary>
    /// <param name="queryable"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Long - quantity of rows returned in the current query.</returns>
    public static Task<long> SqlLongCountAsync(
        this IQueryable queryable,
        CancellationToken cancellationToken = default)
    {
        if (queryable.Provider is InternalQueryProvider i)
        {
            return i.LongCountExecuteAsync(queryable.Expression, cancellationToken);
        }

        return Task.FromResult(queryable.Cast<object>().LongCount());
    }
}
