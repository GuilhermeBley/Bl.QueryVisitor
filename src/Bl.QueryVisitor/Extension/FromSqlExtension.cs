using Microsoft.EntityFrameworkCore;

namespace Bl.QueryVisitor.Extension;

public static class FromSqlExtension
{
    public static IQueryable<TEntity> FromSqlRowE<TEntity>(this DbSet<TEntity> dbSet)
        where TEntity : class
    {
        new Pomelo.EntityFrameworkCore.MySql.Query.ExpressionVisitors.Internal.MySqlQuerySqlGenerator(
            dbSet.EntityType.)

        return dbSet.FromSqlRaw("",);
    }
}
