using System.Collections;

namespace Bl.QueryVisitor.MySql;

/// <summary>
/// Add sql sections to specific places in the query.
/// </summary>
/// <remarks>
/// MYSQL query commands contains these sections: SELECT, FROM, WHERE, GROUP BY, HAVING, ORDER BY and LIMIT.
/// </remarks>
public enum CommandLocaleRegion
{
    /// <summary>
    /// Before selection (SELECT).
    /// </summary>
    /// <remarks>
    ///     <b>Example: </b> {COMMAND_HERE}; SELECT * FROM `table` WHERE `column` = @P1000
    /// </remarks>
    Header,
    /// <summary>
    /// Before having clauses (HAVING).
    /// </summary>
    /// <remarks>
    ///     <b>Example: </b> SELECT * FROM `table` WHERE `column` = @P1000 {COMMAND_HERE} HAVING ...
    /// </remarks>
    BeforeHavingSelection,
    /// <summary>
    /// Before columns and after the 'SELECT'. This command just work with feature 'EnsureAllColumnSet'.
    /// </summary>
    /// <remarks>
    ///     <b>Example: </b> SELECT {COMMAND_HERE} * FROM `table` WHERE `column` = @P1000 HAVING ...
    /// </remarks>
    AfterSelection,

    //
    // After adding a new command, please update the 'ResultWriter' class in the method 'WriteCommandLocale'.
    //
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
