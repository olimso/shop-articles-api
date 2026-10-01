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
        _service = new ArticleService(_repository, new CreateArticleRequestValidator());
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
}
