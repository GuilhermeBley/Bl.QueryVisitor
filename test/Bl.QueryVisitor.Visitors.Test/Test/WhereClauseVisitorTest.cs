using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Bl.QueryVisitor.Extension;

namespace Bl.QueryVisitor.Visitors.Test;

public class WhereClauseVisitorTest
    : TestBase
{
    [Fact]
    public void GetWhereClauses_TryGetWhereClauses_Success()
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
}