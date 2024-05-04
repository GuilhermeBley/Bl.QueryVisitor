namespace Bl.QueryVisitor.Visitors.Test.Test;

public class MethodParamVisitorTest
{
    [Fact]
    public void Translate_CheckNonStaticEquals_SuccessClauseColected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id.Equals(1));

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(1, result.Parameters.Count);
    }

    [Fact]
    public void Translate_CheckStaticEquals_SuccessClauseColected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => int.Equals(model.Id, 1));

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(1, result.Parameters.Count);
    }

    [Fact]
    public void Translate_CheckNonStaticConcat_SuccessClauseColected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Name == string.Concat("abc", "ax", 1));

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (Name = CONCAT(@P1000,@P1001,@P1002))", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckStaticConcat_SuccessClauseColected()
    {
#pragma warning disable CS0253 // Possible unintended reference comparison; right hand side needs cast
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Name == "abc".Concat("ax"));
#pragma warning restore CS0253 // Possible unintended reference comparison; right hand side needs cast

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("abc", result.Parameters.ElementAt(0).Value);
        Assert.Equal("ax", result.Parameters.ElementAt(1).Value);
        Assert.Equal("\nHAVING (Name = CONCAT(@P1000,@P1001))", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckContains_SuccessParametersCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Name.Contains("abc"));

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(1, result.Parameters.Count);
    }

    [Fact]
    public void Translate_CheckContainsAndConcat_SuccessParametersCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Name.Contains("abc"))
            .Where(model => model.Name == string.Concat("abc", "ax", 1));

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (Name LIKE CONCAT('%',@P1000,'%') AND (Name = CONCAT(@P1001,@P1002,@P1003))", result.HavingSql);
    }
}
