namespace Bl.QueryVisitor.Visitors;

public record SimpleQueryTranslatorResult(
    IReadOnlyDictionary<string, object?> Parameters,
    IEnumerable<string> Columns,
    string HavingSql,
    string OrderBySql,
    string LimitSql);
