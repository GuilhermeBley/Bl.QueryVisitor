using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bl.QueryVisitor.Visitors.Test;

public class TestBase
{
    private static IHost _globalHost;

    static TestBase()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureAppConfiguration((host, builder) =>
        {
            builder.AddUserSecrets(typeof(TestBase).Assembly);
        });

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<FakeContext>();
        });

        _globalHost = builder.Build();
    }

    public IServiceProvider Provider
        => _globalHost.Services;

    public IServiceProvider ScopedProvider()
        => Provider.CreateAsyncScope().ServiceProvider;

    public class FakeContext 
        : DbContext
    {
        private readonly IConfiguration _configuration;

        public DbSet<FakeModel> Fakes { get; set; } = null!;

        public FakeContext(IConfiguration configuration) 
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseMySql(
                    connectionString: _configuration["MySql:ConnectionString"].ToString(),
                    ServerVersion.Create(1, 1, 1, Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql));

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FakeModel>(cfg =>
            {
                cfg.HasKey(x => x.Id);
            });
        }
    }

    public class FakeModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime InsertedAt { get; set; }
    }
}
