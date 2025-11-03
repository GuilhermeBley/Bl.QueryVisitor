namespace Bl.QueryVisitor.MySql.Test.TestBase;

using Dapper;
using global::MySql.Data.MySqlClient;
using Testcontainers.MySql;

[CollectionDefinition(CollectionName)]
public class InfrastructureTestFixtureCollection : ICollectionFixture<InfrastructureTestFixture>
{
    public const string CollectionName = "InfrastructureTestCollection";
}

public class InfrastructureTestFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer;

    public string MySqlConnectionString { get; private set; } = null!;

    public InfrastructureTestFixture()
    {
        _mySqlContainer = new MySqlBuilder()
            .WithImage("mysql:8.4")
            .WithDatabase("testdb")
            .WithUsername("root")
            .WithPassword("testpassword")
            .WithDatabase("queryable-test")
            .WithCommand("--sql-mode=ALLOW_INVALID_DATES,STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION") // allow dates '0000-00-00'
            .WithCleanUp(true)
            .Build();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _mySqlContainer.DisposeAsync();
        }
        catch { /* ignore */ }
    }
    public async Task InitializeAsync()
    {
        await _mySqlContainer.StartAsync();

        MySqlConnectionString = _mySqlContainer.GetConnectionString();

        await CreateMySqlSchema();
    }

    private async Task CreateMySqlSchema()
    {
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS `FakeModel` (
                `Id` INT NOT NULL AUTO_INCREMENT,
                `Name` VARCHAR(255) NOT NULL,
                `Value` VARCHAR(255) NULL,
                `InsertedAt` DATETIME NOT NULL,
                `InsertedAtOnlyDate` DATE NOT NULL,
                PRIMARY KEY (`Id`)
            );";

        using var connection = new MySqlConnection(MySqlConnectionString);

        await connection.ExecuteAsync(new CommandDefinition(createTableSql));

        var fakeInsertSql = @"
            INSERT INTO `FakeModel` (`Name`, `Value`, `InsertedAt`, `InsertedAtOnlyDate`)
            VALUES
                ('Test Name 1', 'Value 1', '2023-01-01 10:00:00', '2023-01-01'),
                ('Test Name 2', 'Value 2', '2023-02-01 11:00:00', '2023-02-01'),
                ('Test Name 3', 'Value 3', '2023-03-01 12:00:00', '2023-03-01');";

        await connection.ExecuteAsync(new CommandDefinition(fakeInsertSql));
    }
}
