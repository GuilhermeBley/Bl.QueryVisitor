using Bl.QueryVisitor.Visitors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
            generatedQueryable = new InternalQueryable<TEntity>(generatedQueryable, dbSet.EntityType);
        
        return generatedQueryable;
    }

    // TODO: Set as private
    public class InternalQueryable<TEntity>
        : IQueryable<TEntity>
    {
        private readonly IQueryable<TEntity> _efQueryable;

        /// <summary>
        /// Provider that improves the expressions changes after execution.
        /// </summary>
        private readonly InternalQueryProvider _provider;

        public InternalQueryable(IQueryable<TEntity> other, IEntityType entityType)
        {
            _efQueryable = other;
            _provider = new InternalQueryProvider(other.Provider, entityType);
        }

        public Type ElementType => _efQueryable.ElementType;

        public Expression Expression => _efQueryable.Expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator GetEnumerator()
            => _efQueryable.GetEnumerator();

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
            => _efQueryable.GetEnumerator();
    }

    // TODO: Set as private
    public class InternalQueryProvider 
        : IQueryProvider,
        IAsyncQueryProvider
    {
        private readonly IEntityType _entityType;
        private readonly IAsyncQueryProvider _asyncProvider;

        public InternalQueryProvider(IQueryProvider sqlProvider, IEntityType entityType)
        {
            _asyncProvider = sqlProvider as IAsyncQueryProvider ?? throw new InvalidOperationException("This is not a 'IAsyncQueryProvider'.");
            _entityType = entityType;
        }

        public IQueryable CreateQuery(Expression expression)
            => _asyncProvider.CreateQuery(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => _asyncProvider.CreateQuery<TElement>(expression);

        public object? Execute(Expression expression)
        {
            expression = NormalizeExpression(expression);

            return _asyncProvider.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            expression = NormalizeExpression(expression);

            return _asyncProvider.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            expression = NormalizeExpression(expression);

            return _asyncProvider.ExecuteAsync<TResult>(expression, cancellationToken);
        }

        private Expression NormalizeExpression(Expression expression)
        {
            var visitor = new HavingPerformanceImprovedFromSqlExpressionVisitor(
                _asyncProvider,
                _entityType);

            return visitor.Visit(expression);
        }
    }
}
