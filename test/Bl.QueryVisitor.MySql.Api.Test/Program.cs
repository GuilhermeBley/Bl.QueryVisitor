using Bl.QueryVisitor.Api.Test;
using Dapper;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddOData(cfg =>
    {
        cfg.Select().OrderBy().Filter().SetMaxTop(1000);
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MySqlOptions>(
     builder.Configuration.GetSection("MySqlOptions"));

var app = builder.Build();

await CreateTableAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

async Task CreateTableAsync(IServiceProvider serviceProvider)
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var opt = scope.ServiceProvider.GetRequiredService<IOptions<MySqlOptions>>();
    using var conn = new MySqlConnection(opt.Value.ConnectionString);
    await conn.ExecuteAsync("""
        CREATE TABLE IF NOT EXISTS FakeModel (
            Id INT PRIMARY KEY AUTO_INCREMENT,
            Name VARCHAR(255) NOT NULL DEFAULT '',
            Value DECIMAL(18, 6) NULL,
            InsertedAtOnlyDate DATE NULL,
            InsertedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        """);
}