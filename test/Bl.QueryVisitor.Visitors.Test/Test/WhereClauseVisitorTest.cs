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
        var query = GetFakeContext()
            .Fakes
            .FromSqlRawE("SELECT 1 FROM ALL")
            .AsNoTracking()
            .Where(m => m.Id == 1);

        var queryString = query.ToQueryString();

        Assert.NotEmpty(queryString);
    }
}