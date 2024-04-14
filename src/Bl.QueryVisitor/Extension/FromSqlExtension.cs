using Bl.QueryVisitor;
using Bl.QueryVisitor.Extension;
using Bl.QueryVisitor.Visitors;
using Dapper;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Bl.QueryVisitord.Extension;

public static class FromSqlExtension
{
    public static IFromSqlQueryable<TEntity> QueryAsQueryable<TEntity>(
        this IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        => QueryAsQueryable<TEntity>(
            connection,
            commandDefinition: new CommandDefinition(
                sql,
                parameters : parameters,
                transaction: transaction,
                cancellationToken: cancellationToken));

    public static IFromSqlQueryable<TEntity> QueryAsQueryable<TEntity>(
        this IDbConnection connection,
        CommandDefinition commandDefinition)
        where TEntity : class
    {
        return new InternalQueryable<TEntity>(connection, commandDefinition);
    }

    public static IQueryable<TEntity> SetColumnName<TEntity>(
        this IQueryable<TEntity> current,
        Expression<Func<TEntity>> property,
        string columnName)
    {
        if (current is InternalQueryable<TEntity> internalQueryable)
        {
            var memberName = GetMemberName(property);

            if (internalQueryable.RenamedProperties.ContainsKey(memberName))
                internalQueryable.RenamedProperties.Remove(memberName);

            internalQueryable.RenamedProperties.Add(memberName, columnName);
        }

        return current;
    }

    private static string GetMemberName<T>(Expression<Func<T>> expression)
    {
        MemberExpression? memberExpression = expression.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("Expression is not a member expression.");
        }

        return memberExpression.Member.Name;
    }

    private class InternalQueryable<TEntity>
        : IFromSqlQueryable<TEntity>
    {
        /// <summary>
        /// Provider that improves the expressions changes after execution.
        /// </summary>
        private readonly InternalQueryProvider _provider;
        private readonly Expression _expression;

        /// <summary>
        /// These items are used to replace the 'Property.Name', because it can improve by using index 
        /// </summary>
        public readonly Dictionary<string, string> RenamedProperties;

        public InternalQueryable(
            IDbConnection dbConnection, 
            CommandDefinition commandDefinition,
            Expression? expression = null,
            Dictionary<string, string>? renamedProperties = null)
        {
            RenamedProperties = renamedProperties ?? new();
            _provider = new(dbConnection, commandDefinition, RenamedProperties);
            _expression = expression ?? Expression.Constant(this);
        }

        public Type ElementType => typeof(TEntity);

        public Expression Expression => _expression;

        public IAsyncQueryProvider Provider => _provider;

        IQueryProvider IQueryable.Provider => _provider;

        public IEnumerator GetEnumerator()
        {
            var enumerable = this.Provider.Execute<IEnumerable<TEntity>>(this.Expression);

            return enumerable.GetEnumerator();
        }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
        {
            var enumerable = this.Provider.Execute<IEnumerable<TEntity>>(this.Expression);

            return enumerable.GetEnumerator();
        }
    }

    private class InternalQueryProvider
        : IFromSqlQueryProvider
    {
        private readonly IDbConnection _dbConnection;
        private readonly CommandDefinition _commandDefinition;
        /// <summary>
        /// These items are used to replace the 'Property.Name', because it can improve by using index 
        /// </summary>
        private readonly Dictionary<string, string> _renamedProperties;

        public InternalQueryProvider(
            IDbConnection dbConnection,
            CommandDefinition commandDefinition,
            Dictionary<string, string> renamedProperties)
        {
            _commandDefinition = commandDefinition;
            _dbConnection = dbConnection;
            _renamedProperties = renamedProperties;
        }

        public IQueryable CreateQuery(Expression expression)
            => new InternalQueryable<object>(_dbConnection, _commandDefinition, expression, _renamedProperties);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new InternalQueryable<TElement>(_dbConnection, _commandDefinition, expression, _renamedProperties);

        public object? Execute(Expression expression)
            => Execute<IEnumerable<object>>(expression);

        public TResult Execute<TResult>(Expression expression)
        {
            var tResultType = typeof(TResult);

            if (!tResultType.IsAssignableTo(typeof(IEnumerable)))
                throw new NotSupportedException("The 'TResult' needs to be an 'IEnumerable'.");

            var resultType = tResultType.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            var translator = new SimpleQueryTranslator();

            var result = translator.Translate(expression);

            var completeSql = ResultWriter.WriteSql(_commandDefinition.CommandText, result);

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
            
            var entities = ExecuteDapperQuery<TResult>(_dbConnection, newCommand, resultType);

            return (TResult)CreateEnumerable(resultType, completeSql, entities);
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

            var completeSql = ResultWriter.WriteSql(_commandDefinition.CommandText, result);

            var dbArgs = new DynamicParameters();

            foreach (var parameter in result.Parameters)
                dbArgs.Add(parameter.Key, parameter.Value);

            dbArgs.AddDynamicParams(_commandDefinition.Parameters);

            CancellationTokenSource cts = 
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _commandDefinition.CancellationToken);

            var newCommand =
                new CommandDefinition(
                    commandText: completeSql,
                    parameters: dbArgs,
                    transaction: _commandDefinition.Transaction,
                    commandTimeout: _commandDefinition.CommandTimeout,
                    commandType: _commandDefinition.CommandType,
                    flags: _commandDefinition.Flags,
                    cancellationToken: cts.Token);

            return ExecuteDapperQueryAsync<TResult>(_dbConnection, newCommand, resultType);
        }

        public static TResult ExecuteDapperQueryAsync<TResult>(
            IDbConnection connection, 
            CommandDefinition commandDefinition, 
            Type entityType)
            => ExecuteDapperQuery<TResult>("QueryAsync", connection, commandDefinition, entityType);

        public static TResult ExecuteDapperQuery<TResult>(
            IDbConnection connection,
            CommandDefinition commandDefinition,
            Type entityType)
            => ExecuteDapperQuery<TResult>("Query", connection, commandDefinition, entityType);

        public static TResult ExecuteDapperQuery<TResult>(
            string methodName,
            IDbConnection connection, 
            CommandDefinition commandDefinition, 
            Type entityType)
        {
            try
            {
                // Get the Query method using reflection
                MethodInfo queryAsyncMethod =
                    typeof(SqlMapper).GetMethod(methodName, genericParameterCount: 1, new[] { typeof(IDbConnection), typeof(CommandDefinition) })?
                        .MakeGenericMethod(entityType)
                    ?? throw new InvalidOperationException("Cannot find dapper method 'Query'.");
                
                return (TResult?)queryAsyncMethod.Invoke(null, new object[] { connection, commandDefinition })
                    ?? throw new InvalidOperationException("Invalid 'TResult'.");
            }
            catch (AggregateException e)
            {
                throw e.InnerExceptions.First();
            }
            catch
            {
                throw;
            }
        }

        public static IEnumerable CreateEnumerable(Type entityType, string queryString, object? entities)
        {
            Type listType = typeof(InternalQueringEnumerable<>).MakeGenericType(entityType);

            // Create an instance of the list
            IEnumerable listInstance = (IEnumerable?)Activator.CreateInstance(listType, queryString, entities)
                ?? throw new InvalidOperationException("Failed to create enumerable.");

            return listInstance;
        }
    }
}
