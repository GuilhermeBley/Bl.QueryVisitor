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
            InsertedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP);
        """);
    
    var count = await conn.QueryFirstAsync<long>("SELECT COUNT(*) FROM FakeModel;");

    if (count < 50)
    {
        await conn.ExecuteAsync("""
            INSERT INTO FakeModel (Name, Value, InsertedAtOnlyDate, InsertedAt) VALUES
            ('Alpha Project', 1234.567890, '2024-01-15', '2024-01-15 09:30:45'),
            ('Beta System', 9876.543210, '2024-01-16', '2024-01-16 14:22:18'),
            ('Gamma Module', 456.789123, '2024-01-17', '2024-01-17 11:05:33'),
            ('Delta Component', 2345.678901, '2024-01-18', '2024-01-18 16:48:07'),
            ('Epsilon Service', 876.543219, '2024-01-19', '2024-01-19 08:12:54'),
            ('Zeta Application', 3456.789012, '2024-01-20', '2024-01-20 13:27:41'),
            ('Eta Framework', 765.432198, '2024-01-21', '2024-01-21 10:19:26'),
            ('Theta Library', 4567.890123, '2024-01-22', '2024-01-22 15:33:09'),
            ('Iota Plugin', 654.321987, '2024-01-23', '2024-01-23 07:44:52'),
            ('Kappa Extension', 5678.901234, '2024-01-24', '2024-01-24 12:58:35'),
            ('Lambda Tool', 543.219876, '2024-01-25', '2024-01-25 17:11:28'),
            ('Mu Utility', 6789.012345, '2024-01-26', '2024-01-26 09:24:13'),
            ('Nu Widget', 432.198765, '2024-01-27', '2024-01-27 14:37:56'),
            ('Xi Gadget', 7890.123456, '2024-01-28', '2024-01-28 16:50:39'),
            ('Omicron Device', 321.987654, '2024-01-29', '2024-01-29 08:03:22'),
            ('Pi Machine', 8901.234567, '2024-01-30', '2024-01-30 13:16:05'),
            ('Rho Engine', 219.876543, '2024-01-31', '2024-01-31 15:28:48'),
            ('Sigma Processor', 9012.345678, '2024-02-01', '2024-02-01 10:41:31'),
            ('Tau Controller', 198.765432, '2024-02-02', '2024-02-02 12:54:14'),
            ('Upsilon Manager', 123.456789, '2024-02-03', '2024-02-03 17:07:57'),
            ('Phi Handler', 2345.678912, '2024-02-04', '2024-02-04 09:20:40'),
            ('Chi Interface', 876.543210, '2024-02-05', '2024-02-05 14:33:23'),
            ('Psi Gateway', 3456.789123, '2024-02-06', '2024-02-06 16:46:06'),
            ('Omega Portal', 765.432109, '2024-02-07', '2024-02-07 08:58:49'),
            ('Aurora System', 4567.890234, '2024-02-08', '2024-02-08 13:11:32'),
            ('Nova Platform', 654.321098, '2024-02-09', '2024-02-09 15:24:15'),
            ('Solar Module', 5678.901345, '2024-02-10', '2024-02-10 10:37:58'),
            ('Lunar Component', 543.210987, '2024-02-11', '2024-02-11 12:50:41'),
            ('Stellar Service', 6789.012456, '2024-02-12', '2024-02-12 17:03:24'),
            ('Galactic App', 432.109876, '2024-02-13', '2024-02-13 09:16:07'),
            ('Cosmic Framework', 7890.123567, '2024-02-14', '2024-02-14 14:28:50'),
            ('Quantum Library', 321.098765, '2024-02-15', '2024-02-15 16:41:33'),
            ('Atomic Plugin', 8901.234678, '2024-02-16', '2024-02-16 08:54:16'),
            ('Molecular Extension', 219.087654, '2024-02-17', '2024-02-17 13:06:59'),
            ('Digital Tool', 9012.345789, '2024-02-18', '2024-02-18 15:19:42'),
            ('Virtual Utility', 198.076543, '2024-02-19', '2024-02-19 10:32:25'),
            ('Cloud Widget', 1234.567901, '2024-02-20', '2024-02-20 12:45:08'),
            ('Network Gadget', 987.654321, '2024-02-21', '2024-02-21 17:57:51'),
            ('Data Device', 456.789234, '2024-02-22', '2024-02-22 09:10:34'),
            ('Info Machine', 2345.678023, '2024-02-23', '2024-02-23 14:23:17'),
            ('Tech Engine', 876.543432, '2024-02-24', '2024-02-24 16:36:00'),
            ('Code Processor', 3456.789345, '2024-02-25', '2024-02-25 08:48:43'),
            ('Program Controller', 765.432543, '2024-02-26', '2024-02-26 13:01:26'),
            ('Software Manager', 4567.890456, '2024-02-27', '2024-02-27 15:14:09'),
            ('App Handler', 654.321654, '2024-02-28', '2024-02-28 10:26:52'),
            ('Web Interface', 5678.901567, '2024-02-29', '2024-02-29 12:39:35'),
            ('Mobile Gateway', 543.210765, '2024-03-01', '2024-03-01 17:52:18'),
            ('Desktop Portal', 6789.012678, '2024-03-02', '2024-03-02 09:05:01'),
            ('Server System', 432.109876, '2024-03-03', '2024-03-03 14:17:44'),
            ('Client Platform', 7890.123789, '2024-03-04', '2024-03-04 16:30:27'),
            ('Database Module', 321.098987, '2024-03-05', '2024-03-05 08:43:10'),
            ('Storage Component', 8901.234890, '2024-03-06', '2024-03-06 13:55:53'),
            ('Cache Service', 219.087098, '2024-03-07', '2024-03-07 15:08:36'),
            ('Memory Application', 9012.345901, '2024-03-08', '2024-03-08 10:21:19'),
            ('Processor Framework', 198.076109, '2024-03-09', '2024-03-09 12:34:02'),
            ('Graphics Library', 1234.568012, '2024-03-10', '2024-03-10 17:46:45'),
            ('Audio Plugin', 987.654210, '2024-03-11', '2024-03-11 09:59:28'),
            ('Video Extension', 456.789345, '2024-03-12', '2024-03-12 14:12:11'),
            ('Media Tool', 2345.678134, '2024-03-13', '2024-03-13 16:24:54'),
            ('Content Utility', 876.543321, '2024-03-14', '2024-03-14 08:37:37'),
            ('Document Widget', 3456.789456, '2024-03-15', '2024-03-15 13:50:20'),
            ('File Gadget', 765.432432, '2024-03-16', '2024-03-16 15:03:03'),
            ('Folder Device', 4567.890567, '2024-03-17', '2024-03-17 10:15:46'),
            ('Archive Machine', 654.321543, '2024-03-18', '2024-03-18 12:28:29'),
            ('Backup Engine', 5678.901678, '2024-03-19', '2024-03-19 17:41:12'),
            ('Restore Processor', 543.210654, '2024-03-20', '2024-03-20 09:53:55'),
            ('Sync Controller', 6789.012789, '2024-03-21', '2024-03-21 14:06:38'),
            ('Share Manager', 432.109765, '2024-03-22', '2024-03-22 16:19:21'),
            ('Transfer Handler', 7890.123890, '2024-03-23', '2024-03-23 08:32:04'),
            ('Download Interface', 321.098876, '2024-03-24', '2024-03-24 13:44:47'),
            ('Upload Gateway', 8901.234901, '2024-03-25', '2024-03-25 15:57:30'),
            ('Stream Portal', 219.087987, '2024-03-26', '2024-03-26 10:10:13'),
            ('Broadcast System', 9012.345012, '2024-03-27', '2024-03-27 12:22:56'),
            ('Receive Platform', 198.076098, '2024-03-28', '2024-03-28 17:35:39'),
            ('Send Module', 1234.568123, '2024-03-29', '2024-03-29 09:48:22'),
            ('Process Component', 987.654321, '2024-03-30', '2024-03-30 14:01:05'),
            ('Analyze Service', 456.789456, '2024-03-31', '2024-03-31 16:13:48'),
            ('Compute Application', 2345.678245, '2024-04-01', '2024-04-01 08:26:31'),
            ('Calculate Framework', 876.543432, '2024-04-02', '2024-04-02 13:39:14'),
            ('Generate Library', 3456.789567, '2024-04-03', '2024-04-03 15:51:57'),
            ('Parse Plugin', 765.432543, '2024-04-04', '2024-04-04 10:04:40'),
            ('Validate Extension', 4567.890678, '2024-04-05', '2024-04-05 12:17:23'),
            ('Verify Tool', 654.321654, '2024-04-06', '2024-04-06 17:30:06'),
            ('Test Utility', 5678.901789, '2024-04-07', '2024-04-07 09:42:49'),
            ('Debug Widget', 543.210765, '2024-04-08', '2024-04-08 14:55:32'),
            ('Monitor Gadget', 6789.012890, '2024-04-09', '2024-04-09 16:08:15'),
            ('Log Device', 432.109876, '2024-04-10', '2024-04-10 08:20:58'),
            ('Track Machine', 7890.123901, '2024-04-11', '2024-04-11 13:33:41'),
            ('Report Engine', 321.098987, '2024-04-12', '2024-04-12 15:46:24'),
            ('Alert Processor', 8901.234012, '2024-04-13', '2024-04-13 10:59:07'),
            ('Notify Controller', 219.087098, '2024-04-14', '2024-04-14 12:11:50'),
            ('Message Manager', 9012.345123, '2024-04-15', '2024-04-15 17:24:33'),
            ('Communication Handler', 198.076109, '2024-04-16', '2024-04-16 09:37:16'),
            ('Connection Interface', 1234.568234, '2024-04-17', '2024-04-17 14:49:59'),
            ('Session Gateway', 987.654432, '2024-04-18', '2024-04-18 16:02:42'),
            ('Security Portal', 456.789567, '2024-04-19', '2024-04-19 08:15:25'),
            ('Authentication System', 2345.678356, '2024-04-20', '2024-04-20 13:28:08'),
            ('Authorization Platform', 876.543543, '2024-04-21', '2024-04-21 15:40:51'),
            ('Encryption Module', 3456.789678, '2024-04-22', '2024-04-22 10:53:34'),
            ('Decryption Component', 765.432654, '2024-04-23', '2024-04-23 12:06:17');
            """);
    }
}