using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors.Test;

public class WhereClauseVisitorTest
{
    [Fact]
    public void GetWhereClauses_TryGetWhereClauses_Success()
    {
        var query = Enumerable.Empty<bool>()
            .AsQueryable()
            .Where(x => x == true || x == false && x == false);
        var visitor = new WhereClauseVisitor();

        var results = visitor.GetWhereClauses(query.Expression);

        Assert.NotEmpty(results);
    }
}