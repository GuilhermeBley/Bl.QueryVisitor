using Bl.QueryVisitor.Visitors;
using System.Text;

namespace Bl.QueryVisitor;

internal static class ResultWriter
{
    public static string WriteSql(string? sql, SimpleQueryTranslatorResult result)
    {
        var builder = new StringBuilder();

        sql = sql?.Trim(' ', '\n', ';') ?? string.Empty;

        builder.Append(sql);
        
        builder.Append(result.HavingSql);
        
        builder.Append(result.OrderBySql);
        
        builder.Append(result.LimitSql);

        if (result.Columns.Any())
            FormatWithAliases(result.Columns, builder);

        builder.Append(';');

        return builder.ToString();
    }

    private static StringBuilder FormatWithAliases(IEnumerable<string> columns, StringBuilder builder, string aliases = "tableasdlkasmd", char nameSeparator = '`')
    {
        aliases = string.Concat(nameSeparator, aliases, nameSeparator);

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
}
