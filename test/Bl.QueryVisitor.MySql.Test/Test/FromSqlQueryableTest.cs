using Bl.QueryVisitor.Extension;
using MySql.Data.MySqlClient;
using System.Data;

namespace Bl.QueryVisitor.MySql.Test.Test;

public class FromSqlQueryableTest
{
    [Fact]
    public void Execute_TryExecuteWhereWithProvider_Success()
    {
        var connection = CreateConnection();

        var result = connection.QueryAsQueryable<FakeModel>("SELECT * FROM `queryable-test`.FakeModel")
            .Where(model => model.Id == 1);

        var executedResult = result.Provider.Execute(result.Expression);

        Assert.NotNull(executedResult);
    }

    [Fact]
    public void Execute_TryExecuteOrderWithProvider_Success()
    {
        var connection = CreateConnection();

        var result = connection.QueryAsQueryable<FakeModel>("SELECT * FROM `queryable-test`.FakeModel")
            .OrderByDescending(model => model.Id)
            .ThenBy(model => model.Name);

        var executedResult = result.Provider.Execute(result.Expression);

        Assert.NotNull(executedResult);
    }

    [Fact]
    public void Execute_TryExecuteLimitWithProvider_Success()
    {
        var connection = CreateConnection();

        var queryable = connection.QueryAsQueryable<FakeModel>("SELECT * FROM `queryable-test`.FakeModel")
            .Take(1)
            .Skip(1);

        Assert.NotEmpty(queryable);
    }

    private static IDbConnection CreateConnection()
    {
        return new MySqlConnection("server=127.0.0.1;port=3310;user id=root;password=root;persistsecurityinfo=True;database=queryable-test;default command timeout=600;SslMode=None");
    }
}