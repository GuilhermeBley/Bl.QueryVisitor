using Bl.QueryVisitord.Extension;
using Dapper;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace Bl.QueryVisitor.Api.Test.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly IOptions<MySqlOptions> _options;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IOptions<MySqlOptions> options,
        ILogger<TestController> logger)
    {
        _options = options;
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    [EnableQuery(EnsureStableOrdering = false)]
    public ActionResult<IQueryable<FakeModel>> Get()
    {
        var queryable =
            CreateConnection()
            .QueryAsQueryable<FakeModel>(new CommandDefinition(
                "SELECT 1 FROM table"));

        return Ok(queryable);
    }

    [HttpGet("2")]
    [EnableQuery(EnsureStableOrdering = false)]
    public ActionResult<IQueryable<FakeModel>> Get2()
    {
        var queryable =
            Enumerable.Range(0,20)
            .Select(n => new FakeModel { Id = n, Name = "name" , InsertedAt = DateTime.Now })
            .ToList()
            .AsQueryable<FakeModel>();

        return Ok(queryable);
    }

    private MySqlConnection CreateConnection()
        => new MySqlConnection(_options.Value.ConnectionString);

    public class FakeModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime InsertedAt { get; set; }
    }
}
