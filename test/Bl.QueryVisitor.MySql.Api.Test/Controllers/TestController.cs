using Bl.QueryVisitor.Extension;
using Dapper;
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
    public ActionResult<IQueryable<FakeModel>> Get(
        [FromServices] ODataQueryOptions<FakeModel> options)
    {
        var queryable =
            CreateConnection()
            .SqlAsQueryable<FakeModel>(new CommandDefinition(
                "SELECT * FROM `queryable-test`.FakeModel"));
        
        var result = options.ApplyTo(queryable);
        _logger.LogInformation(
            result.ToSqlText());
        return Ok(result);
    }

    [HttpGet("2")]
    [EnableQuery(EnsureStableOrdering = false)]
    public ActionResult<IQueryable<FakeModel>> Get2(
        [FromServices]ODataQueryOptions<FakeModel> options)
    {
        var queryable =
            Enumerable.Range(0,20)
            .Select(n => new FakeModel { Id = n, Name = "name" , InsertedAt = DateTime.Now })
            .ToList()
            .AsQueryable<FakeModel>();

        var result = options.ApplyTo(queryable);
        _logger.LogInformation(
            result.ToSqlText());
        return Ok(result);
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
