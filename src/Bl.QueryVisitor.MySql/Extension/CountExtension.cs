using Bl.QueryVisitor.Extension;

namespace Bl.QueryVisitor.MySql.Extension;
public static class CountExtension
{
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
