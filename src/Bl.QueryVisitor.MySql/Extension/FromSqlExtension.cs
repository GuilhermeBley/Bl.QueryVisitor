﻿using Bl.QueryVisitor.Visitors;
using Dapper;
using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Bl.QueryVisitor.Extension;

/// <summary>
/// This extension class provides methods to generate 
/// queryables by SQL commands in Dapper.
/// </summary>
public static class FromSqlExtension
{
    /// <summary>
    /// This method generates a MYSQL queryable to operate with query methods.
    /// </summary>
    /// <remarks>
    ///     <para>The queryable supports the following methods:</para>
    ///     <list type="bullet">
    ///     <item>Where</item>
    ///     <item>OrderBy</item>
    ///     <item>OrderByDescending</item>
    ///     <item>Select</item>
    ///     </list>
    /// </remarks>
    public static IFromSqlQueryable<TEntity> SqlAsQueryable<TEntity>(
        this IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        => SqlAsQueryable<TEntity>(
            connection,
            commandDefinition: new CommandDefinition(
                sql,
                parameters : parameters,
                transaction: transaction,
                cancellationToken: cancellationToken));

    /// <summary>
    /// This method generates a MYSQL queryable to operate with query methods.
    /// </summary>
    /// <remarks>
    ///     <para>The queryable supports the following methods:</para>
    ///     <list type="bullet">
    ///     <item>Where</item>
    ///     <item>OrderBy</item>
    ///     <item>OrderByDescending</item>
    ///     <item>Select</item>
    ///     </list>
    /// </remarks>
    public static IFromSqlQueryable<TEntity> SqlAsQueryable<TEntity>(
        this IDbConnection connection,
        CommandDefinition commandDefinition)
        where TEntity : class
    {
        return new InternalQueryable<TEntity>(connection, commandDefinition, typeof(TEntity));
    }

    /// <summary>
    /// Add direct columns related to object properties or fields.
    /// </summary>
    /// <remarks>
    ///     <para>Useful in performance scenarios, as long as the <paramref name="columnName"/> could be the direct table column.</para>
    ///     <para>The property name will be changed to the column name in the MYSQL query.</para>
    /// </remarks>
    public static IQueryable<TEntity> SetColumnName<TEntity, TIn>(
        this IQueryable<TEntity> current,
        Expression<Func<TEntity, TIn>> property,
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

    public static string ToSqlText(this IQueryable queryable)
    {
        if (queryable is IFromSqlTextQuery textQuery)
            return textQuery.ToSqlText();

        return string.Empty;
    }

    private static string GetMemberName<T, TIn>(Expression<Func<T, TIn>> expression)
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
        private readonly CommandDefinition _commandDefinition;
        private readonly Type _model;
        public Type ModelType => _model;

        /// <summary>
        /// These items are used to replace the 'Property.Name', because it can improve by using index 
        /// </summary>
        public readonly Dictionary<string, string> RenamedProperties;

        public InternalQueryable(
            IDbConnection dbConnection,
            CommandDefinition commandDefinition,
            Type model,
            Expression? expression = null,
            Dictionary<string, string>? renamedProperties = null)
        {
            _commandDefinition = commandDefinition;
            RenamedProperties = renamedProperties ?? new();
            _provider = new(dbConnection, commandDefinition, RenamedProperties, model);
            _expression = expression ?? Expression.Constant(this);
            _model = model;
        }

        public Type ElementType => typeof(TEntity);

        public Expression Expression => _expression;

        public IFromSqlQueryProvider Provider => _provider;
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

        public async IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var results =
                await this.Provider.ExecuteAsync<Task<IEnumerable<TEntity>>>(this.Expression, cancellationToken);

            foreach (var result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return result;
            }
        }

        public string ToSqlText()
        {
            var translator = new SimpleQueryTranslator(RenamedProperties);

            var result = translator.Translate(Expression);

            var completeSql = ResultWriter.WriteSql(_commandDefinition.CommandText, result);

            return completeSql;
        }
    }

    private class InternalQueryProvider
        : IFromSqlQueryProvider
    {
        private readonly IDbConnection _dbConnection;
        private readonly CommandDefinition _commandDefinition;
        private readonly Type _model;
        /// <summary>
        /// These items are used to replace the 'Property.Name', because it can improve by using index 
        /// </summary>
        private readonly Dictionary<string, string> _renamedProperties;

        public InternalQueryProvider(
            IDbConnection dbConnection,
            CommandDefinition commandDefinition,
            Dictionary<string, string> renamedProperties,
            Type model)
        {
            _commandDefinition = commandDefinition;
            _dbConnection = dbConnection;
            _renamedProperties = renamedProperties;
            _model = model;
        }

        public IQueryable CreateQuery(Expression expression)
            => new InternalQueryable<object>(
                dbConnection: _dbConnection, 
                commandDefinition: _commandDefinition, 
                model: this._model,
                expression: expression, 
                renamedProperties: _renamedProperties);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new InternalQueryable<TElement>(
                dbConnection: _dbConnection,
                commandDefinition: _commandDefinition,
                model: this._model,
                expression: expression,
                renamedProperties: _renamedProperties);

        public object? Execute(Expression expression)
            => Execute<IEnumerable<object>>(expression);

        public TResult Execute<TResult>(Expression expression)
        {
            var tResultType = typeof(TResult);

            if (!tResultType.IsAssignableTo(typeof(IEnumerable)))
                throw new NotSupportedException("The 'TResult' needs to be an 'IEnumerable'.");

            var resultType = tResultType.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            var translator = new SimpleQueryTranslator(_renamedProperties);

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

            return entities;
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            if (!typeof(TResult).IsAssignableTo(typeof(Task)))
                throw new NotSupportedException("It's required a 'Task' result.");

            var resultType = this._model;

            var translator = new SimpleQueryTranslator(_renamedProperties);

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

        private static TResult ExecuteDapperQueryAsync<TResult>(
            IDbConnection connection, 
            CommandDefinition commandDefinition, 
            Type entityType)
            => ExecuteDapperQuery<TResult>("QueryAsync", connection, commandDefinition, entityType);

        private static TResult ExecuteDapperQuery<TResult>(
            IDbConnection connection,
            CommandDefinition commandDefinition,
            Type entityType)
            => ExecuteDapperQuery<TResult>("Query", connection, commandDefinition, entityType);

        private static TResult ExecuteDapperQuery<TResult>(
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
    }
}