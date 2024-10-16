using Bl.QueryVisitor.Visitors;
using Dapper;
using System.Collections;
using System.Data;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Extension;

internal class InternalQueryable<TEntity>
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

        return string.Concat(
            string.Join(',', translator.Parameters),
            '\n',
            completeSql);
    }
}