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

    [Fact]
    public void GetWhereClauses_TryGetWhereClausesFromModelWithOr_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 0 || model.Name == string.Empty || model.InsertedAt == DateTime.UtcNow);
        var visitor = new WhereClauseVisitor();

        var results = visitor.GetWhereClauses(query.Expression);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void GetWhereClauses_TryGetWhereClausesFromModelWithAnd_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 0 && model.Name == string.Empty && model.InsertedAt == DateTime.UtcNow);
        var visitor = new WhereClauseVisitor();

        var results = visitor.GetWhereClauses(query.Expression);

        Assert.Equal(3, results.Count);
    }

    private class FakeModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime InsertedAt { get; set; }
    }
}