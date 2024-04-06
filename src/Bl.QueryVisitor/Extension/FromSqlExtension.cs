using Bl.QueryVisitor.Visitors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Extension;

public static class FromSqlExtension
{
    public static IQueryable<TEntity> FromSqlRowE<TEntity>(
        this DbSet<TEntity> dbSet,
        [NotParameterized] string sql,
        params object[] parameters)
        where TEntity : class
    {
        // TODO: Throw if contains more than one '{having}'

        IQueryable<TEntity>? generatedQueryable = dbSet.FromSqlRaw(sql, parameters);
        
        if (!sql.Contains("{having}", StringComparison.OrdinalIgnoreCase))
            generatedQueryable = new InternalQueryable<TEntity>(generatedQueryable);

        return generatedQueryable;
    }

    public class InternalQueryable<TEntity>
        : IQueryable<TEntity>
    {
        private readonly IQueryable<TEntity> _efQueryable;

        /// <summary>
        /// Provider that improves the expressions changes after execution.
        /// </summary>
        private readonly InternalQueryProvider _provider;

        public InternalQueryable(IQueryable<TEntity> other)
        {
            _efQueryable = other;
            _provider = new InternalQueryProvider(other.Provider);
        }

        public Type ElementType => _efQueryable.ElementType;

        public Expression Expression => _efQueryable.Expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator GetEnumerator()
            => _efQueryable.GetEnumerator();

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
            => _efQueryable.GetEnumerator();
    }

    public class InternalQueryProvider 
        : IQueryProvider,
        IAsyncQueryProvider
    {
        private readonly IQueryProvider _sqlProvider;

        public InternalQueryProvider(IQueryProvider sqlProvider)
        {
            _sqlProvider = sqlProvider;
        }

        public IQueryable CreateQuery(Expression expression)
            => _sqlProvider.CreateQuery(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => _sqlProvider.CreateQuery<TElement>(expression);

        public object? Execute(Expression expression)
        {
            expression = NormalizeExpression(expression);

            return _sqlProvider.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            expression = NormalizeExpression(expression);

            return _sqlProvider.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            if (_sqlProvider is not IAsyncQueryProvider asyncQueryProvider)
                throw new InvalidOperationException("This is not a 'IAsyncQueryProvider'.");

            expression = NormalizeExpression(expression);

            return asyncQueryProvider.ExecuteAsync<TResult>(expression, cancellationToken);
        }

        private static Expression NormalizeExpression(Expression expression)
        {
            var visitor = new HavingPerformanceImprovedFromSqlExpressionVisitor();

            return visitor.Visit(expression);
        }
    }
}
