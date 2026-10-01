using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShopArticles.Core.Contracts;
using ShopArticles.Core.Models;
using ShopArticles.Core.Repositories;
using ShopArticles.Infrastructure.InMemory;
using ShopArticles.Infrastructure.Persistence;

namespace ShopArticles.UnitTests;

public class InMemoryArticleRepositoryTests : ArticleRepositoryTests
{
    protected override Task<IArticleRepository> CreateRepositoryAsync() =>
        Task.FromResult<IArticleRepository>(new InMemoryArticleRepository());
}

// Runs every test against a real SQLite file, created fresh for each test.
public class SqliteArticleRepositoryTests : ArticleRepositoryTests
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"articles-test-{Guid.NewGuid():N}.db");

    protected override async Task<IArticleRepository> CreateRepositoryAsync()
    {
        var options = new DbContextOptionsBuilder<ArticlesDbContext>().UseSqlite($"Data Source={_databasePath}").Options;
        await using (var db = new ArticlesDbContext(options))
            await db.Database.MigrateAsync();

        return new EfArticleRepository(new TestDbContextFactory(options));
    }

    public override Task DisposeAsync()
    {
        // Pooled connections keep the file open, so release them before deleting it.
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            File.Delete(_databasePath + suffix);
        return Task.CompletedTask;
    }

    private class TestDbContextFactory(DbContextOptions<ArticlesDbContext> options) : IDbContextFactory<ArticlesDbContext>
    {
        public ArticlesDbContext CreateDbContext() => new(options);
    }
}

// Behaviour every IArticleRepository must have, whatever the storage.
public abstract class ArticleRepositoryTests : IAsyncLifetime
{
    private IArticleRepository _repository = null!;

    protected abstract Task<IArticleRepository> CreateRepositoryAsync();

    public async Task InitializeAsync() => _repository = await CreateRepositoryAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;

    private static Article NewArticle(string name, string? category = null) => new()
    {
        Name = name,
        Description = "description",
        Category = category,
        Price = 0,
    };

    [Fact]
    public async Task Add_assigns_increasing_ids()
    {
        var first = await _repository.AddAsync(NewArticle("A"));
        var second = await _repository.AddAsync(NewArticle("B"));

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
    }

    [Fact]
    public async Task Concurrent_adds_get_unique_ids()
    {
        var created = await Task.WhenAll(
            Enumerable.Range(0, 200).Select(i => Task.Run(() => _repository.AddAsync(NewArticle($"Article {i}")))));

        Assert.Equal(200, created.Select(a => a.Id).Distinct().Count());
    }

    [Fact]
    public async Task GetById_returns_null_for_unknown_id()
    {
        Assert.Null(await _repository.GetByIdAsync(42));
    }

    [Fact]
    public async Task Search_by_name_is_partial_and_case_insensitive()
    {
        await _repository.AddAsync(NewArticle("Branded Memory Stick"));
        await _repository.AddAsync(NewArticle("Branded Drinking Mug"));
        await _repository.AddAsync(NewArticle("Plain Pen"));

        var result = await _repository.SearchAsync(new ArticleSearchCriteria(Name: "bRANDED"));

        Assert.Equal(["Branded Memory Stick", "Branded Drinking Mug"], result.Select(a => a.Name));
    }

    [Fact]
    public async Task Search_by_category_requires_the_complete_name()
    {
        await _repository.AddAsync(NewArticle("Stick", "USB flash drive"));
        await _repository.AddAsync(NewArticle("Mug", "Mug"));

        Assert.Single(await _repository.SearchAsync(new ArticleSearchCriteria(Category: "USB flash drive")));
        Assert.Empty(await _repository.SearchAsync(new ArticleSearchCriteria(Category: "USB")));
    }

    [Fact]
    public async Task Search_combines_criteria_with_and()
    {
        await _repository.AddAsync(NewArticle("Branded Memory Stick", "USB flash drive"));
        await _repository.AddAsync(NewArticle("Branded Drinking Mug", "Mug"));

        var result = await _repository.SearchAsync(new ArticleSearchCriteria(Name: "branded", Category: "Mug"));

        Assert.Equal("Branded Drinking Mug", Assert.Single(result).Name);
    }

    [Fact]
    public async Task Returned_articles_do_not_share_state_with_the_store()
    {
        var created = await _repository.AddAsync(NewArticle("Original"));
        created.Name = "Changed by caller";

        var stored = await _repository.GetByIdAsync(created.Id);

        Assert.Equal("Original", stored!.Name);
    }

    [Fact]
    public async Task Search_treats_wildcard_characters_literally()
    {
        await _repository.AddAsync(NewArticle("100% cotton shirt"));
        await _repository.AddAsync(NewArticle("1000 stickers"));

        var result = await _repository.SearchAsync(new ArticleSearchCriteria(Name: "100%"));

        Assert.Equal("100% cotton shirt", Assert.Single(result).Name);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("17.89")]
    public async Task Price_comes_back_exactly_as_stored(string price)
    {
        var article = NewArticle("Priced");
        article.Price = decimal.Parse(price, CultureInfo.InvariantCulture);

        var created = await _repository.AddAsync(article);
        var stored = await _repository.GetByIdAsync(created.Id);

        // Compared as text, because 5m and 5.0m are equal as numbers but serialize differently in JSON.
        Assert.Equal(price, stored!.Price.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Stores_all_fields()
    {
        var created = await _repository.AddAsync(new Article
        {
            Name = "Branded Memory Stick",
            Description = "Branded 16 GB memory stick",
            Category = "USB flash drive",
            Price = 17.89m,
            Currency = "NOK",
        });

        var stored = await _repository.GetByIdAsync(created.Id);

        Assert.NotNull(stored);
        Assert.Equal(("Branded Memory Stick", "Branded 16 GB memory stick", "USB flash drive", 17.89m, "NOK"),
            (stored.Name, stored.Description, stored.Category, stored.Price, stored.Currency));
    }
}
