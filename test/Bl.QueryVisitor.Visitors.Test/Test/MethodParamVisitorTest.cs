namespace Bl.QueryVisitor.Visitors.Test.Test;

public class MethodParamVisitorTest
{

    [Fact]
    public void Translate_TryGetWhereConstClauses_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id.Equals(1));

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(1, result.Parameters.Count);
    }
}
