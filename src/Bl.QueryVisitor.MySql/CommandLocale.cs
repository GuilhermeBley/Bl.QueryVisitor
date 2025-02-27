using System.Collections;

namespace Bl.QueryVisitor.MySql;


public enum CommandLocaleRegion
{
    /// <summary>
    /// Before selection.
    /// </summary>
    Header
}

/// <summary>
/// This command will be wrote in the specific locale and after it will be inserted a jump row.
/// </summary>
public record CommandLocale(
    CommandLocaleRegion Region,
    string SqlCommand);

public class CommandLocaleArray
    : IReadOnlyList<CommandLocale>
{
    private readonly List<CommandLocale> _commands = new List<CommandLocale>();

    public CommandLocale this[int index] => _commands[index];

    public int Count => _commands.Count;

    public CommandLocaleArray(IEnumerable<CommandLocale> commands)
        => _commands.AddRange(commands);

    public IEnumerator<CommandLocale> GetEnumerator()
    {
        return _commands.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _commands.GetEnumerator();
    }

    public IReadOnlyList<CommandLocale> GetByRegion(CommandLocaleRegion region)
    {
        return _commands.FindAll(x => x.Region == region);
    }
}
