namespace Bl.QueryVisitor.MySql.Config;

internal interface IQueryContextProvider
{
    InternalQueryContext Context { get; }
}

internal record InternalColumnConfig(
    string MemberName,
    string ColumnName,
    bool Hidden);

internal class InternalQueryContext
{
    public Type Type { get; }

    /// <summary>
    /// These items are used to replace the 'Property.Name', because it can improve by using index 
    /// </summary>
    public Dictionary<string, InternalColumnConfig> RenamedProperties { get; }
        = new(StringComparer.OrdinalIgnoreCase);
    public List<CommandLocale> AdditionalCommands { get; } = new List<CommandLocale>();
    /// <summary>
    /// This flag marks that the <see cref="RenamedProperties"/> should have all the class properties to the query.
    /// </summary>
    public bool EnsureAllColumnsMapped = false;

    public InternalQueryContext(Type type)
    {
        Type = type;
    }
}
