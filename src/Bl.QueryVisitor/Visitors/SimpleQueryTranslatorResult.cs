namespace Bl.QueryVisitor.Visitors;

public record SimpleQueryTranslatorResult(
    IReadOnlyDictionary<string, object?> Parameters,
    string HavingSql,
    string OrderBySql,
    string LimitSql);
