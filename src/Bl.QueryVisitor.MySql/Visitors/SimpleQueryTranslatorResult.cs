using Bl.QueryVisitor.MySql;

namespace Bl.QueryVisitor.Visitors;

public record SimpleQueryTranslatorResult(
    IReadOnlyDictionary<string, object?> Parameters,
    IEnumerable<string> Columns,
    CommandLocaleArray AdditionalCommands,
    string SelectSql,
    string HavingSql,
    string OrderBySql,
    string LimitSql);
