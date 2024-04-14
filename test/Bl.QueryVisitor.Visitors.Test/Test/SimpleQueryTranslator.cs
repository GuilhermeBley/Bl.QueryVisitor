namespace Bl.QueryVisitor.Visitors.Test;

public class SimpleQueryTranslator
{
    [Fact]
    public void Translate_TryGetWhereConstClauses_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1 && model.Name == "asc" || model.Name == "asc213")
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.NotNull(result);
    }

    [Fact]
    public void Translate_TryGetWhereVariablesClauses_Success()
    {
        var time = new DateTime(1900, 1, 1);

        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == time)
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var queryString = visitor.Translate(query.Expression).HavingSql;

        Assert.NotEmpty(queryString);
    }

    [Fact]
    public void Translate_TryGetWhereFunctionsClauses_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == new DateTime(1900, 1, 1))
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var queryString = visitor.Translate(query.Expression).HavingSql;

        Assert.Equal("\nHAVING (InsertedAt = @P1001)", queryString, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_TryGetWhereFunctionsClausesCheckNewDateTimeData_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == new DateTime(1900, 1, 1))
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(new DateTime(1900, 1, 1), result.Parameters.First().Value);
    }

    [Fact]
    public void Translate_CheckEmptyWhere_SuccessEmpty()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(string.Empty, result.HavingSql);
    }

    [Fact]
    public void Translate_CheckSelect_SuccessColumnsCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Select(model => new { model.Name });

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Single(result.Columns);
    }

    [Fact]
    public void Translate_CheckSelectWithOtherExpressions_SuccessColumnsCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100)
            .Select(model => new { model.Name }); ;

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.NotEmpty(result.HavingSql);
        Assert.NotEmpty(result.OrderBySql);
        Assert.NotEmpty(result.LimitSql);
        Assert.Single(result.Columns);
    }

    [Fact]
    public void Translate_CheckSelectWithouNewObject_SuccessColumnsCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Select(model =>  model.Name);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Single(result.Columns);
    }

    [Fact]
    public void Translate_CheckEmptyOrder_SuccessEmpty()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(string.Empty, result.OrderBySql);
    }

    [Fact]
    public void Translate_CheckEmptyLimit_SuccessEmpty()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(string.Empty, result.LimitSql);
    }

    [Fact]
    public void Translate_CheckThenByAfterOrder_SuccessThenBy()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .ThenBy(model => model.Id)
            .ThenByDescending(model => model.InsertedAt);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nORDER BY Name ASC, Id ASC, InsertedAt DESC", result.OrderBySql);
    }

    [Fact]
    public void Translate_CheckDoubleOrdering_FailedToExecuteDoubleOrderByNotSupported()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .OrderBy(model => model.Id);

        var visitor = new Visitors.SimpleQueryTranslator();

        Action act = () => visitor.Translate(query.Expression);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Translate_CheckLimitText_SuccessLimitFound()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .Take(1);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nLIMIT 1", result.LimitSql);
    }

    [Fact]
    public void Translate_CheckLimitOffsetText_SuccessLimitFound()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .Take(1)
            .Skip(1);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nLIMIT 1 OFFSET 1", result.LimitSql);
    }
}