namespace Bl.QueryVisitor.MySql.Providers;

/// <summary>
/// This class provides the name for columns based on the direct columns.
/// </summary>
public class ColumnNameProvider
{
    private readonly IReadOnlyDictionary<string, string> _renamedProperties;

    public ColumnNameProvider(IReadOnlyDictionary<string, string> directColumns)
    {
        _renamedProperties = directColumns;
    }

    public string GetColumnName(string column)
    {
        if (_renamedProperties.Count == 0)
            return TransformColumn(column);

        _renamedProperties.TryGetValue(column, out var parsedColumnName);

        return TransformColumn(parsedColumnName ?? column);
    }

    protected virtual string TransformColumn(string column)
    {
        return column;
    }
}
