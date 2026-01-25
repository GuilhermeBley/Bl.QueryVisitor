using Bl.QueryVisitor.MySql;
using Bl.QueryVisitor.MySql.Config;
using Bl.QueryVisitor.Visitors;
using Dapper;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Bl.QueryVisitor.Extension;

internal class InternalQueryable<TEntity>
    : IFromSqlQueryable<TEntity>,
    IQueryContextProvider
{
    /// <summary>
    /// Provider that improves the expressions changes after execution.
    /// </summary>
    private InternalQueryProvider _provider => new(_dbConnection, _commandDefinition, Context);
    private readonly Expression _expression;
    private readonly IDbConnection _dbConnection;
    private readonly CommandDefinition _commandDefinition;

    public InternalQueryContext Context { get; }
    public Type ModelType => Context.Type;

    public InternalQueryable(
        IDbConnection dbConnection,
        CommandDefinition commandDefinition,
        Type model,
        Expression? expression = null,
        InternalQueryContext? context = null)
    {
        _dbConnection = dbConnection;
        _commandDefinition = commandDefinition;
        Context = context ?? new(model);
        _expression = expression ?? Expression.Constant(this);
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
        var translator = Provider.GenerateTranslator();

        var result = translator.Translate(Expression);

        var completeSql = ResultWriter.WriteSql(_commandDefinition.CommandText, result);

        return string.Concat(
            string.Join(',', translator.Parameters),
            '\n',
            completeSql);
    }
}