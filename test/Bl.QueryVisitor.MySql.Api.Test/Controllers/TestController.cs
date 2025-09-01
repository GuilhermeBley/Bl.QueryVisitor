using Bl.QueryVisitor.Extension;
using Bl.QueryVisitor.MySql.Api.Test.Controllers;
using Bl.QueryVisitor.MySql.Extension;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
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
                "SELECT *, null as Value FROM `queryable-test`.FakeModel a"));
        
        var result = options.ApplyTo(queryable);
        _logger.LogInformation(
            result.ToSqlText());
        return Ok(result);
    }

    [HttpGet("ColumnName/$count")]
    public async Task<ActionResult<long>> GetWithColumnNameCount(
        [FromServices] ODataQueryOptions<FakeModel> options,
        CancellationToken cancellationToken = default)
    {
        IQueryable queryable =
            CreateConnection()
            .SqlAsQueryable<FakeModel>(new CommandDefinition(
                "FROM `queryable-test`.FakeModel a"))
            .SetColumnName(e => e.Id, "a.Id")
            .SetColumnName(e => e.InsertedAt, "a.InsertedAt")
            .SetColumnName(e => e.InsertedAtOnlyDate, "a.InsertedAtOnlyDate")
            .SetColumnName(e => e.Name, "a.Name")
            .EnsureAllColumnSet();

        if (options.Filter != null)
            queryable = options.Filter.ApplyTo(queryable, new ODataQuerySettings());

        var count = await queryable.SqlLongCountAsync(cancellationToken);
        return Ok(count);
    }

    [HttpGet("ColumnName")]
    [EnableQuery]
    public async Task<ActionResult<IQueryable<FakeModel>>> GetWithColumnName(
        [FromServices] ODataQueryOptions<FakeModel> options)
    {
        var queryable =
            CreateConnection()
            .SqlAsQueryable<FakeModel>(new CommandDefinition(
                "FROM `queryable-test`.FakeModel a"))
            .SetColumnName(e => e.Id, "a.Id")
            .SetColumnName(e => e.InsertedAt, "a.InsertedAt")
            .SetColumnName(e => e.InsertedAtOnlyDate, "a.InsertedAtOnlyDate")
            .SetColumnName(e => e.Name, "a.Name")
            .EnsureAllColumnSet();

        var exp = options.ApplyTo(queryable).Expression;

        var sql = options.ApplyTo(queryable).ToSqlText();
        var exptxt = exp.ToString();
        _logger.LogInformation(exptxt);
        _logger.LogInformation(sql);

        await Task.CompletedTask;

        return Ok(queryable);
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
    {
        var conn = new MySqlConnection(_options.Value.ConnectionString);
        return conn;
    }

    public class FakeModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Value { get; set; }
        public DateOnly? InsertedAtOnlyDate { get; set; }
        public DateTime InsertedAt { get; set; }
    }
}
