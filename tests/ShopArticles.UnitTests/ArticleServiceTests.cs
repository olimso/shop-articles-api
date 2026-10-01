using FluentValidation;
using ShopArticles.Core.Contracts;
using ShopArticles.Core.Services;
using ShopArticles.Core.Validation;
using ShopArticles.Infrastructure.InMemory;

namespace ShopArticles.UnitTests;

public class ArticleServiceTests
{
    private readonly InMemoryArticleRepository _repository = new();
    private readonly ArticleService _service;

    public ArticleServiceTests()
    {
        _service = new ArticleService(_repository, new CreateArticleRequestValidator(), new UpdateArticleRequestValidator());
    }

    [Fact]
    public async Task Create_stores_article_and_returns_it_with_generated_id()
    {
        var response = await _service.CreateAsync(new CreateArticleRequest
        {
            Name = "Branded Memory Stick",
            Description = "Branded 16 GB memory stick",
            Category = "USB flash drive",
            Price = 17.89m,
            Currency = "nok",
        });

        Assert.Equal(1, response.ArticleId);
        Assert.Equal("NOK", response.Currency);
        Assert.NotNull(await _repository.GetByIdAsync(response.ArticleId));
    }

    [Fact]
    public async Task Create_with_invalid_request_throws_and_stores_nothing()
    {
        var invalid = new CreateArticleRequest { Name = "No description", Price = 1, Currency = "EUR" };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(invalid));
        Assert.Empty(await _repository.SearchAsync(new ArticleSearchCriteria()));
    }

    [Fact]
    public async Task CreateMany_stores_all_and_returns_them_in_request_order()
    {
        var requests = Enumerable.Range(1, 100)
            .Select(i => new CreateArticleRequest { Name = $"Article {i}", Description = "Bulk", Price = i, Currency = "EUR" })
            .ToList();

        var created = await _service.CreateManyAsync(requests);

        Assert.Equal(requests.Select(r => r.Name), created.Select(a => a.Name));
        Assert.Equal(Enumerable.Range(1, 100).Select(i => (long)i), created.Select(a => a.ArticleId).Order());
        Assert.Equal(100, (await _repository.SearchAsync(new ArticleSearchCriteria())).Count);
    }

    [Fact]
    public async Task CreateMany_with_one_invalid_request_throws_and_stores_nothing()
    {
        var requests = new List<CreateArticleRequest>
        {
            new() { Name = "Valid", Description = "Valid", Price = 1, Currency = "EUR" },
            new() { Name = "Missing currency", Description = "Invalid", Price = 1 },
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateManyAsync(requests));

        Assert.Equal("[1].Currency", Assert.Single(exception.Errors).PropertyName);
        Assert.Empty(await _repository.SearchAsync(new ArticleSearchCriteria()));
    }

    [Fact]
    public async Task CreateMany_with_null_item_throws_validation_error_for_that_position()
    {
        var requests = new List<CreateArticleRequest>
        {
            new() { Name = "Valid", Description = "Valid", Price = 1, Currency = "EUR" },
            null!,
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateManyAsync(requests));

        Assert.Equal("[1]", Assert.Single(exception.Errors).PropertyName);
        Assert.Empty(await _repository.SearchAsync(new ArticleSearchCriteria()));
    }

    [Fact]
    public async Task CreateMany_with_empty_list_throws()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateManyAsync([]));
    }
}
