namespace Bl.QueryVisitor.Visitors.Test;

internal record FakeComplexModel(
    Guid MyGuid,
    object MyObj,
    DateTimeOffset DateTimeOffset);

internal record FakeModel(
    int Id,
    string Name, 
    DateTime InsertedAt);
