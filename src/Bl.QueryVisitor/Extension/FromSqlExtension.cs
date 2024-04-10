using Bl.QueryVisitor.Visitors;
using Dapper;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Bl.QueryVisitor.Extension;

public static class FromSqlExtension
{
    public static IQueryable<TEntity> QueryAsQueryable<TEntity>(
        this IDbConnection connection,
        string sql,
        object? parameters,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        => QueryAsQueryable<TEntity>(
            connection,
            sql,
            new CommandDefinition(
                sql,
                parameters : parameters,
                transaction: transaction,
                cancellationToken: cancellationToken));

    public static IQueryable<TEntity> QueryAsQueryable<TEntity>(
        this IDbConnection connection,
        CommandDefinition commandDefinition)
        where TEntity : class
    {
        return new InternalQueryable<TEntity>(connection, commandDefinition);
    }

    private class InternalQueryable<TEntity>
        : IQueryable<TEntity>,
        IQueryingEnumerable
    {
        private readonly IQueryable<TEntity> _queryable;
        private readonly CommandDefinition _commandDefinition;
        /// <summary>
        /// Provider that improves the expressions changes after execution.
        /// </summary>
        private readonly InternalQueryProvider _provider;

        public InternalQueryable(IDbConnection dbConnection, CommandDefinition commandDefinition)
        {
            _commandDefinition = commandDefinition;
            _queryable = Enumerable.Empty<TEntity>().AsQueryable();
            _provider = new(dbConnection, commandDefinition);
        }

        public Type ElementType => _queryable.ElementType;

        public Expression Expression => _queryable.Expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator GetEnumerator()
            => _queryable.GetEnumerator();

        public string ToQueryString()
        {
            var translator = new SimpleQueryTranslator();

            var result = translator.Translate(Expression);

            var completeSql =
                string.Concat(_commandDefinition.CommandText, result.HavingSql, result.OrderBySql, result.LimitSql);

            return completeSql;
        }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
            => _queryable.GetEnumerator();
    }

    private class InternalQueryProvider
        : IQueryProvider,
        IAsyncQueryProvider
    {
        private readonly IDbConnection _dbConnection;
        private readonly CommandDefinition _commandDefinition;

        public InternalQueryProvider(IDbConnection dbConnection, CommandDefinition commandDefinition)
        {
            _commandDefinition = commandDefinition;
            _dbConnection = dbConnection;
        }

        public IQueryable CreateQuery(Expression expression)
            => Enumerable.Empty<object>().AsQueryable();

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => Enumerable.Empty<TElement>().AsQueryable();

        public object? Execute(Expression expression)
            => Execute<IEnumerable<dynamic>>(expression);

        public TResult Execute<TResult>(Expression expression)
        {
            if (!typeof(TResult).IsAssignableTo(typeof(IEnumerable)))
                throw new NotSupportedException("The 'TResult' needs to be an 'IEnumerable'.");

            var resultType = typeof(TResult).GetGenericArguments().FirstOrDefault() ?? typeof(object);

            var translator = new SimpleQueryTranslator();

            var result = translator.Translate(expression);

            var completeSql =
                string.Concat(_commandDefinition.CommandText, result.HavingSql, result.OrderBySql, result.LimitSql);

            var dbArgs = new DynamicParameters();

            foreach (var parameter in result.Parameters)
                dbArgs.Add(parameter.Key, parameter.Value);

            dbArgs.AddDynamicParams(_commandDefinition.Parameters);

            var newCommand =
                new CommandDefinition(
                    commandText: completeSql,
                    parameters: dbArgs,
                    transaction: _commandDefinition.Transaction,
                    commandTimeout: _commandDefinition.CommandTimeout,
                    commandType: _commandDefinition.CommandType,
                    flags: _commandDefinition.Flags,
                    cancellationToken: _commandDefinition.CancellationToken);

            return ExecuteDapperQuery<TResult>(_dbConnection, newCommand, resultType);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            if (!typeof(TResult).IsAssignableTo(typeof(Task)))
                throw new NotSupportedException("It's required a 'Task' result.");

            var taskType = typeof(TResult).GetGenericArguments().Single();

            var resultType = taskType.GetGenericArguments()
                .FirstOrDefault() 
                ?? typeof(object);

            var translator = new SimpleQueryTranslator();

            var result = translator.Translate(expression);

            var completeSql =
                string.Concat(_commandDefinition.CommandText, result.HavingSql, result.OrderBySql, result.LimitSql);

            var dbArgs = new DynamicParameters();

            foreach (var parameter in result.Parameters)
                dbArgs.Add(parameter.Key, parameter.Value);

            dbArgs.AddDynamicParams(_commandDefinition.Parameters);

            var newCommand =
                new CommandDefinition(
                    commandText: completeSql,
                    parameters: dbArgs,
                    transaction: _commandDefinition.Transaction,
                    commandTimeout: _commandDefinition.CommandTimeout,
                    commandType: _commandDefinition.CommandType,
                    flags: _commandDefinition.Flags,
                    cancellationToken: _commandDefinition.CancellationToken);

            return ExecuteDapperQueryAsync<TResult>(_dbConnection, newCommand, resultType);
        }

        public static TResult ExecuteDapperQueryAsync<TResult>(IDbConnection connection, CommandDefinition commandDefinition, Type entityType)
        {
            // Get the QueryAsync method using reflection
            MethodInfo queryAsyncMethod =
                typeof(IDbConnection).GetMethod("QueryAsync", new[] { typeof(IDbConnection), typeof(CommandDefinition) })?
                .MakeGenericMethod(entityType)
                ?? throw new InvalidOperationException("Cannot find dapper method 'QueryAsync'.");

            return (TResult?)queryAsyncMethod.Invoke(null, new object[] { connection, commandDefinition })
                ?? throw new InvalidOperationException("Invalid 'TResult'.");
        }

        public static TResult ExecuteDapperQuery<TResult>(IDbConnection connection, CommandDefinition commandDefinition, Type entityType)
        {
            // Get the Query method using reflection
            MethodInfo queryAsyncMethod =
                typeof(IDbConnection).GetMethod("Query", new[] { typeof(IDbConnection), typeof(CommandDefinition) })?
                .MakeGenericMethod(entityType)
                ?? throw new InvalidOperationException("Cannot find dapper method 'Query'.");

            return (TResult?)queryAsyncMethod.Invoke(null, new object[] { connection, commandDefinition })
                ?? throw new InvalidOperationException("Invalid 'TResult'.");
        }
    }
}
