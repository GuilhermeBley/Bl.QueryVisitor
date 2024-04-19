namespace Bl.QueryVisitor.Visitors.Test;

internal record FakeComplexModel(
    Guid MyGuid,
    object MyObj,
    DateTimeOffset DateTimeOffset,
    DateTimeOffset? DateTimeOffsetWithUnderlineType);

internal record FakeModel(
    int Id,
    string Name, 
    DateTime InsertedAt);
