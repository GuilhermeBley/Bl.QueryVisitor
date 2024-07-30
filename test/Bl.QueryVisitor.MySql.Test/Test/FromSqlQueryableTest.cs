using Bl.QueryVisitor.Extension;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;

namespace Bl.QueryVisitor.MySql.Test.Test;

public class FromSqlQueryableTest
{
    [Fact]
    public void Execute_TryExecuteWhereWithProvider_Success()
    {
        using var connection = CreateConnection();

        var queryable = connection.SqlAsQueryable<FakeModel>("SELECT * FROM `queryable-test`.FakeModel")
            .Where(model => model.Id == 1);

        var executedResult = queryable.Provider.Execute(queryable.Expression);

        Assert.NotNull(executedResult);
    }

    [Fact]
    public void Execute_TryExecuteWhereWithParameters_Success()
    {
        using var connection = CreateConnection();

        var queryable = connection.SqlAsQueryable<FakeModel>(
            "SELECT * FROM `queryable-test`.FakeModel WHERE Name = @Name",
            parameters: new
            {
                Name = "Name"
            })
            .Where(model => model.Id == 1);

        var executedResult = queryable.Provider.Execute(queryable.Expression);

        Assert.NotNull(executedResult);
    }

    [Fact]
    public void Execute_CheckeWhereWithParameters_SuccessDoubleParams()
    {
        using var connection = CreateConnection();

        var queryable = connection.SqlAsQueryable<FakeModel>(
            "SELECT * FROM `queryable-test`.FakeModel WHERE Name = @Name",
            parameters: new
            {
                Name = "Name"
            })
            .Where(model => model.Id == 1);

        var queryString = queryable.ToSqlText();

        Assert.Equal(3, queryString.Where(c => c == '@').Count());
    }

    [Fact]
    public void Execute_TryExecuteOrderWithProvider_Success()
    {
        using var connection = CreateConnection();

        var queryable = connection.SqlAsQueryable<FakeModel>("SELECT * FROM `queryable-test`.FakeModel")
            .OrderByDescending(model => model.Id)
            .ThenBy(model => model.Name);

        var executedResult = queryable.Provider.Execute(queryable.Expression);

        Assert.NotNull(executedResult);
    }

    [Fact]
    public void Execute_TryExecuteLimitWithProvider_Success()
    {
        using var connection = CreateConnection();

        var queryable = connection.SqlAsQueryable<FakeModel>("SELECT * FROM `queryable-test`.FakeModel")
            .Take(1)
            .Skip(1);

        Assert.NotEmpty(queryable);
    }

    [Fact]
    public async void ExecuteAsync_TryExecuteLimitWithProvider_Success()
    {
        using var connection = CreateConnection();

        var queryable = connection.SqlAsQueryable<FakeModel>("SELECT * FROM `queryable-test`.FakeModel")
            .Take(1)
            .Skip(1);

        var asyncProvider = (IFromSqlQueryProvider)queryable.Provider;

        var results = await asyncProvider.ExecuteAsync<Task<IEnumerable<FakeModel>>>(queryable.Expression);

        Assert.NotEmpty(results);
    }

    [Fact]
    public async void ExecuteAsync_ExecuteWithNewObjectSelection()
    {
        using var connection = CreateConnection();

        var queryable = connection.SqlAsQueryable<FakeModel>("SELECT * FROM `queryable-test`.FakeModel")
            .Take(1)
            .Skip(1)
            .Select(e => new
            {
                e.Name
            });

        var asyncProvider = (IFromSqlQueryProvider)queryable.Provider;

        var results = await asyncProvider.ExecuteAsync<Task<IEnumerable<object>>>(queryable.Expression);

        Assert.NotEmpty(results);
    }

    private static IDbConnection CreateConnection()
    {
        return new MySqlConnection("server=127.0.0.1;port=3310;user id=root;password=root;persistsecurityinfo=True;database=queryable-test;default command timeout=600;SslMode=None");
    }
}