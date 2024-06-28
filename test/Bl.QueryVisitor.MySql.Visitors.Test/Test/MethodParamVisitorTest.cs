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

        Assert.Equal("\nHAVING (Name LIKE CONCAT('%',@P1000,'%')) AND (Name = CONCAT(@P1001,@P1002,@P1003))", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckDateTimeNow_SuccessDateParsedToSql()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == DateTime.Now);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (InsertedAt = NOW())", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckDateTimeUtcNow_SuccessDateParsedToSql()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == DateTime.UtcNow);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (InsertedAt = UTC_TIMESTAMP())", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckDateTimegGuid_SuccessGuidParsedToSql()
    {
        var query = Enumerable.Empty<FakeComplexModel>()
            .AsQueryable()
            .Where(model => model.MyGuid == Guid.NewGuid());

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (MyGuid = UUID())", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckDateYear_SuccessYearFunc()
    {
        var query = Enumerable.Empty<FakeComplexModel>()
            .AsQueryable()
            .Where(model => model.DateTimeOffset.Year > 1);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (Year(DateTimeOffset) > @P1000)", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckDateMonth_SuccessMonthFunc()
    {
        var query = Enumerable.Empty<FakeComplexModel>()
            .AsQueryable()
            .Where(model => model.DateTimeOffset.Month > 1);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (Month(DateTimeOffset) > @P1000)", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckDateDay_SuccessDayFunc()
    {
        var query = Enumerable.Empty<FakeComplexModel>()
            .AsQueryable()
            .Where(model => model.DateTimeOffset.Day > 1);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (Day(DateTimeOffset) > @P1000)", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckNullValue_SuccessNullCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Name != null);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING (Name IS NOT NULL)", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckIfStatment_SuccessConvertedToIfMysql()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => (model.Name == null ? true : false));

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING IF((Name IS NULL),@P1000,@P1001)", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckIfStatmentTwice_SuccessConvertedToIfMysql()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => (model.Name == null ? true : false))
            .Where(model => (model.InsertedAt > new DateTime(2001,1,1) ? true : false));

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nHAVING IF((Name IS NULL),@P1000,@P1001) AND IF((InsertedAt > @P1002),@P1003,@P1004)", result.HavingSql);
    }
}
