using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopArticles.Core.Repositories;
using ShopArticles.Infrastructure;
using ShopArticles.Infrastructure.InMemory;
using ShopArticles.Infrastructure.Persistence;

namespace ShopArticles.UnitTests;

public class DependencyInjectionTests
{
    private static IArticleRepository ResolveRepository(string? provider, string? connectionString = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = provider,
                ["ConnectionStrings:Articles"] = connectionString,
            })
            .Build();

        using var services = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        return services.GetRequiredService<IArticleRepository>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("InMemory")]
    [InlineData("inmemory")]
    [InlineData(" INMEMORY ")]
    public void InMemory_is_chosen_by_default_and_ignores_case_and_whitespace(string? provider)
    {
        Assert.IsType<InMemoryArticleRepository>(ResolveRepository(provider));
    }

    [Theory]
    [InlineData("Sqlite")]
    [InlineData("sqlite")]
    [InlineData(" SQLITE ")]
    [InlineData("\tSqlite\n")]
    public void Sqlite_ignores_case_and_whitespace(string provider)
    {
        // Resolving doesn't open the database, so the file is never created.
        Assert.IsType<EfArticleRepository>(ResolveRepository(provider, "Data Source=unused.db"));
    }

    [Fact]
    public void Sqlite_without_connection_string_throws()
    {
        Assert.Throws<InvalidOperationException>(() => ResolveRepository("Sqlite"));
    }

    [Fact]
    public void Unknown_provider_throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ResolveRepository("Postgres"));
        Assert.Contains("'Postgres'", exception.Message);
    }
}
