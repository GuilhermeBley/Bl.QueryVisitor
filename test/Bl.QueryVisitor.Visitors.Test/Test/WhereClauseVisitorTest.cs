namespace Bl.QueryVisitor.Visitors.Test;

public class WhereClauseVisitorTest
    : TestBase
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

        var visitor = new SimpleQueryTranslator();

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

        var visitor = new SimpleQueryTranslator();

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

        var visitor = new SimpleQueryTranslator();

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

        var visitor = new SimpleQueryTranslator();

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

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(string.Empty, result.HavingSql);
    }

    [Fact]
    public void Translate_CheckEmptyOrder_SuccessEmpty()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .Skip(100)
            .Take(100);

        var visitor = new SimpleQueryTranslator();

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

        var visitor = new SimpleQueryTranslator();

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

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nORDER BY Name ASC, Id ASC, InsertedAt DESC", result.OrderBySql);
    }
}