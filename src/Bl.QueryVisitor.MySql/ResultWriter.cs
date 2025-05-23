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

        WriteNextCommandCheckingDubleRowJump(builder, result.WhereSql);

        WriteCommandLocale(builder, result.AdditionalCommands, CommandLocaleRegion.BeforeHavingSelection);

        WriteNextCommandCheckingDubleRowJump(builder, result.HavingSql);

        WriteNextCommandCheckingDubleRowJump(builder, result.OrderBySql);
        
        WriteNextCommandCheckingDubleRowJump(builder, result.LimitSql);

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
            var commandNormalized = command.SqlCommand.Trim(';', ' ');

            switch (region)
            {
                case CommandLocaleRegion.Header:
                    builder.Append(commandNormalized);
                    builder.Append(';');
                    builder.Append('\n');
                    break;
                case CommandLocaleRegion.BeforeHavingSelection:
                    builder.Append('\n');
                    builder.Append(commandNormalized);
                    builder.Append('\n');
                    break;
                default:
                    throw new NotImplementedException($"Command local {region} is not suppoted.");
            }

            builder.Append('\n');
        }
    }

    private static void WriteNextCommandCheckingDubleRowJump(StringBuilder builder, string command)
    {
        command = command?.Trim('\n') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(command)) return;

        var lastChar = builder.Length == 0 ? '\0' : builder[builder.Length - 1];
        if (lastChar != '\n')
        {
            builder.Append('\n');
        }
        builder.Append(command);
    }
}
