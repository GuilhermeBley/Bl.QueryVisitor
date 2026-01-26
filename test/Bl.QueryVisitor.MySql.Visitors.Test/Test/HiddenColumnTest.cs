using Bl.QueryVisitor.Extension;
using Bl.QueryVisitor.Visitors;
using Bl.QueryVisitor.Visitors.Test;
using Dapper;

namespace Bl.QueryVisitor.MySql.Visitors.Test.Test;

public class HiddenColumnTest
{
    [Fact]
    public void SetColumnName_SetHiddenColumn()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("FROM User"))
            .AsQueryable()
            .EnsureAllColumnSet()
            .SetColumnName(e => e.InsertedAt, "IF(InsertedAt > 0, InsertedAt, NULL)")
            .SetColumnName(e => e.Name, "NULL")
            .SetColumnName(e => e.Id, "f.Id", true)
            .Where(e => e.InsertedAt > new DateTime(2025, 1, 1));

        var result = query.ToSqlText();

        Assert.Contains("SELECT", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Id", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("f.Id", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetColumnName_SetHiddenColumnTwiceAndPreserveLatest()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("FROM User"))
            .AsQueryable()
            .EnsureAllColumnSet()
            .SetColumnName(e => e.InsertedAt, "f.InsertedAt")
            .SetColumnName(e => e.Name, "f.Name")
            .SetColumnName(e => e.Id, "f.Id", true);

        var firstResult = query.ToSqlText();

        query = query.SetColumnName(e => e.Id, "f.Id2", false);

        var latestResult = query.ToSqlText();

        Assert.Contains("SELECT", firstResult, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Id", firstResult, StringComparison.OrdinalIgnoreCase);

        // Checking the latest result
        Assert.Contains("Id", latestResult, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("f.Id2", latestResult, StringComparison.OrdinalIgnoreCase);
    }
}
