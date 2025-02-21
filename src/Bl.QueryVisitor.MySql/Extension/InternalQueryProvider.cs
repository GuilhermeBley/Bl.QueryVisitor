﻿using Bl.QueryVisitor.MySql;
using Bl.QueryVisitor.MySql.Exceptions;
using Bl.QueryVisitor.Visitors;
using Dapper;
using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Bl.QueryVisitor.Extension;

internal class InternalQueryProvider
    : IFromSqlQueryProvider
{
    private readonly IDbConnection _dbConnection;
    private readonly CommandDefinition _commandDefinition;
    private readonly Type _model;
    /// <summary>
    /// These items are used to replace the 'Property.Name', because it can improve by using index 
    /// </summary>
    private readonly Dictionary<string, string> _renamedProperties;
    private readonly bool _ensureAllColumnsMapped;

    public InternalQueryProvider(
        IDbConnection dbConnection,
        CommandDefinition commandDefinition,
        Dictionary<string, string> renamedProperties,
        bool ensureAllColumnsMapped,
        Type model)
    {
        _commandDefinition = commandDefinition;
        _dbConnection = dbConnection;
        _renamedProperties = renamedProperties;
        _ensureAllColumnsMapped = ensureAllColumnsMapped;
        _model = model;
    }

    public IQueryable CreateQuery(Expression expression)
        => new InternalQueryable<object>(
            dbConnection: _dbConnection,
            commandDefinition: _commandDefinition,
            model: this._model,
            expression: expression,
            renamedProperties: _renamedProperties)
        {
            EnsureAllColumnsMapped = _ensureAllColumnsMapped
        };

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new InternalQueryable<TElement>(
            dbConnection: _dbConnection,
            commandDefinition: _commandDefinition,
            model: this._model,
            expression: expression,
            renamedProperties: _renamedProperties)
        {
            EnsureAllColumnsMapped = _ensureAllColumnsMapped
        };

    public object? Execute(Expression expression)
        => Execute<IEnumerable<object>>(expression);

    public TResult Execute<TResult>(Expression expression)
    {
        var tResultType = typeof(TResult);

        if (!tResultType.IsAssignableTo(typeof(IEnumerable)))
            throw new NotSupportedException("The 'TResult' needs to be an 'IEnumerable'.");

        var resultType = this._model;

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

        var entities = ExecuteAndTransformData(_dbConnection, expression, newCommand, resultType, translator.ItemTranslator);

        return (TResult)entities;
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

        return GetListTaskToExecuteAndTransformDataAsync<TResult>(
            _dbConnection,
            expression,
            newCommand,
            resultType,
            translator.ItemTranslator);
    }

    /// <summary>
    /// Execute method ExecuteAndTransformDataAsync with generic
    /// </summary>
    /// <typeparam name="TResult">Task of something</typeparam>
    private static TResult GetListTaskToExecuteAndTransformDataAsync<TResult>(
        IDbConnection connection,
        Expression expression,
        CommandDefinition commandDefinition,
        Type entityType,
        IItemTranslator translator)
    {
        try
        {
            var listType = typeof(TResult).GetGenericArguments().First();

            // Get the Query method using reflection
            MethodInfo executeAndTransformDataAsync =
                typeof(InternalQueryProvider).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == nameof(InternalQueryProvider.ExecuteAndTransformDataAsync))?
                    .MakeGenericMethod(listType)
                ?? throw new InvalidOperationException("Cannot find dapper method 'Query'.");

            return (TResult?)executeAndTransformDataAsync.Invoke(null, new object[] { connection, expression, commandDefinition, entityType, translator })
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

    private static async Task<T> ExecuteAndTransformDataAsync<T>(
        IDbConnection connection,
        Expression expression,
        CommandDefinition commandDefinition,
        Type entityType,
        IItemTranslator translator)
    {
        try
        {
            var entitiesCollection
                = await ExecuteDapperQueryAsync(connection, commandDefinition, entityType);

            var expectedType = typeof(T).GetGenericArguments()
                .First(); // first type argument of the list

            IList results = CreateList(expectedType, entitiesCollection.Count());
            foreach (var item in entitiesCollection)
            {
                var translatedItem = translator.TransformItem(item);

                results.Add(translatedItem);
            }

            return (T)results;
        }
        catch (Exception e)
        {
            throw new QueryException("Failed to execute command.", expression, commandDefinition.CommandText, e);
        }
    }

    private static IEnumerable ExecuteAndTransformData(
        IDbConnection connection,
        Expression expression,
        CommandDefinition commandDefinition,
        Type entityType,
        IItemTranslator translator)
    {
        try
        {

            var entitiesCollection = ExecuteDapperQuery(connection, commandDefinition, entityType);

            IList results = CreateList(entityType, entitiesCollection.Count());
            foreach (var item in entitiesCollection)
            {
                results.Add(translator.TransformItem(item));
            }

            return results;
        }
        catch (Exception e)
        {
            throw new QueryException("Failed to execute command.", expression, commandDefinition.CommandText, e);
        }
    }

    private static Task<IEnumerable<object>> ExecuteDapperQueryAsync(
        IDbConnection connection,
        CommandDefinition commandDefinition,
        Type entityType)
        => connection.QueryAsync(entityType, commandDefinition);

    private static IEnumerable<object> ExecuteDapperQuery(
        IDbConnection connection,
        CommandDefinition commandDefinition,
        Type entityType)
    {
        try
        {
            // Get the Query method using reflection
            MethodInfo queryAsyncMethod =
                typeof(SqlMapper).GetMethod(nameof(Dapper.SqlMapper.Query), genericParameterCount: 1, new[] { typeof(IDbConnection), typeof(CommandDefinition) })?
                    .MakeGenericMethod(entityType)
                ?? throw new InvalidOperationException("Cannot find dapper method 'Query'.");

            return (IEnumerable<object>?)queryAsyncMethod.Invoke(null, new object[] { connection, commandDefinition })
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

    private static IList CreateList(Type entityType, int count)
    {
        Type listType = typeof(List<>).MakeGenericType(entityType);
        IList? list = (IList?)Activator.CreateInstance(listType, new object?[] { count });

        return list ?? throw new InvalidOperationException();
    }
}
