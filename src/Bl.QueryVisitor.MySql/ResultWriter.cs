using Bl.QueryVisitor.MySql;
using Bl.QueryVisitor.Visitors;
using System.Text;

namespace Bl.QueryVisitor;

internal static class ResultWriter
{
    public static string WriteSql(string? sql, SimpleQueryTranslatorResult result)
    {
        var builder = new StringBuilder();

        sql = sql?.Trim(' ', '\n', ';') ?? string.Empty;

        WriteCommandLocale(builder, result.AdditionalCommands, CommandLocaleRegion.Header);

        builder.Append(result.SelectSql);

        builder.Append(sql);

        builder.Append(result.WhereSql);

        builder.Append(result.HavingSql);
        
        builder.Append(result.OrderBySql);
        
        builder.Append(result.LimitSql);

        if (result.Columns.Any())
            FormatWithAliases(result.Columns, builder);

        builder.Append(';');

        return builder.ToString();
    }

    private static StringBuilder FormatWithAliases(
        IEnumerable<string> columns, 
        StringBuilder builder, 
        char nameSeparator = '`')
    {
        var aliases = GetUniqueAliases(
            sql: builder.ToString(),
            aliases: "t",
            nameSeparator: nameSeparator);

        columns = columns.Select(column => string.Concat(aliases, '.', nameSeparator, column, nameSeparator));

        // Inserting before SQL command already added in the builder.
        builder.Insert(
            0,
            string.Concat(
                "SELECT ", 
                string.Join(", ", columns), // columns
                " FROM ("));

        // After sql ...
        builder.Append(") AS ");

        builder.Append(aliases);

        return builder;
    }

    private static string GetUniqueAliases(string sql, string aliases, char nameSeparator)
    {
        for (uint aliasesIndex = 0; aliasesIndex < uint.MaxValue; aliasesIndex++)
        {
            var uniqueAlises = string.Concat(nameSeparator, aliases, aliasesIndex, nameSeparator);

            if (sql.Contains(uniqueAlises, StringComparison.OrdinalIgnoreCase))
                continue;

            return uniqueAlises;
        }

        throw new ArgumentException("No alises unique value were found.", nameof(aliases));
    }

    private static void WriteCommandLocale(
        StringBuilder builder, 
        CommandLocaleArray commandLocales, 
        CommandLocaleRegion region)
    {
        var commands = commandLocales.GetByRegion(region);

        if (commands.Count == 0) return;

        foreach (var command in commands)
        {
            builder.Append(command.SqlCommand);
            builder.Append(';');
            builder.Append('\n');
        }
    }
}
