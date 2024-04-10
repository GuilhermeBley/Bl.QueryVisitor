using Microsoft.EntityFrameworkCore.Query;
using System.Collections;

namespace Bl.QueryVisitor.Extension;

public class InternalQueringEnumerable<TEntity>
    : IEnumerable<TEntity>,
    IQueryingEnumerable
{
    private readonly string _query;
    private IEnumerable<TEntity> _entities;

    public InternalQueringEnumerable(string query, object? entities)
        : this(query, (IEnumerable<TEntity>?)entities ?? Enumerable.Empty<TEntity>())
    {
    }

    public InternalQueringEnumerable(string query, IEnumerable<TEntity> entities)
    {
        _query = query;
        _entities = entities;
    }

    public IEnumerator<TEntity> GetEnumerator()
        => _entities.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _entities.GetEnumerator();

    public string ToQueryString()
    {
        return _query;   
    }
}
