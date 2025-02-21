using Dapper;
using System.Data;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Extension;

/// <summary>
/// This extension class provides methods to generate 
/// queryables by SQL commands in Dapper.
/// </summary>
public static partial class FromSqlExtension
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
                parameters: parameters,
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
    public static IFromSqlQueryable<TEntity> SetColumnName<TEntity, TIn>(
        this IFromSqlQueryable<TEntity> current,
        Expression<Func<TEntity, TIn>> property,
        string columnName)
        => (IFromSqlQueryable<TEntity>)SetColumnName(
            current: (IQueryable<TEntity>)current,
            property: property,
            columnName: columnName);

    /// <summary>
    /// Add direct SQL commands to relate with the object properties.
    /// </summary>
    /// <remarks>
    ///     <para>Useful in performance scenarios, as long as the <paramref name="sqlCommand"/> could be related with the direct table column.</para>
    ///     <para>The property name will be changed to the sql command in the MYSQL query.</para>
    /// </remarks>
    public static IQueryable<TEntity> SetColumnAsSql<TEntity, TIn>(
        this IQueryable<TEntity> current,
        Expression<Func<TEntity, TIn>> property,
        string sqlCommand)
    {
        if (current.Provider is not InternalQueryable<TEntity> internalQuery) return current;

        internalQuery.EnsureAllColumnsMapped = true;

        return SetColumnName(current, property, sqlCommand);
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

    /// <summary>
    /// Add a conversion to the returned models in the queryable.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="current">The current query</param>
    /// <param name="conversion">The conversion model, where you'll change the current model.</param>
    /// <returns>An <see cref="IQueryable"/> whose elements are the same but using the conversion./returns>
    public static IQueryable<TEntity> AddConversion<TEntity>(
        this IQueryable<TEntity> current,
        Action<TEntity> conversion)
    {
        return current.Select(e => GetValueAndConvert(e, conversion));
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

    private static TEntity GetValueAndConvert<TEntity>(
        TEntity entity,
        Action<TEntity> conversion)
    {
        try
        {
            conversion(entity);

            return entity;
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse entity '{typeof(TEntity).FullName}'.");
            throw;
        }
    }
}