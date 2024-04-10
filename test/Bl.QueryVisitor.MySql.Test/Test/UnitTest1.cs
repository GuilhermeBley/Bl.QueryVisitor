using Bl.QueryVisitor.Extension;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace Bl.QueryVisitor.MySql.Test.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var connection = CreateConnection();

        var result = connection.QueryAsQueryable<FakeModel>(new CommandDefinition("SELECT 1 FROM TABLE"))
            .Where(model => model.Id == 1);

        var executedResult = result.Provider.Execute(result.Expression);
    }

    private static IDbConnection CreateConnection()
    {
        return new MySqlConnection();
    }
}