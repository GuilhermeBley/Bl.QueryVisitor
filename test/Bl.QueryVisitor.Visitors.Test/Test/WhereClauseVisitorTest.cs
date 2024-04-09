using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Bl.QueryVisitor.Extension;

namespace Bl.QueryVisitor.Visitors.Test;

public class WhereClauseVisitorTest
    : TestBase
{
    [Fact]
    public void GetWhereClauses_TryGetWhereConstClauses_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1 && model.Name == "asc" || model.Name == "asc213")
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new SimpleQueryTranslator();

        var queryString = visitor.Translate(query.Expression);

        Assert.NotEmpty(queryString);
    }

    [Fact]
    public void GetWhereClauses_TryGetWhereVariablesClauses_Success()
    {
        var time = new DateTime(1900, 1, 1);

        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == time)
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new SimpleQueryTranslator();

        var queryString = visitor.Translate(query.Expression);

        Assert.NotEmpty(queryString);
    }

    [Fact]
    public void GetWhereClauses_TryGetWhereFunctionsClauses_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == new DateTime(1900, 1, 1))
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new SimpleQueryTranslator();

        var queryString = visitor.Translate(query.Expression);

        Assert.Equal("(InsertedAt = @P1001)", queryString, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWhereClauses_TryGetWhereFunctionsClausesCheckNewDateTimeData_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == new DateTime(1900, 1, 1))
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new SimpleQueryTranslator();

        var queryString = visitor.Translate(query.Expression);

        Assert.Equal(new DateTime(1900, 1, 1), visitor.Parameters.First().Value);
    }
}