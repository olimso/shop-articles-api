using ShopArticles.Core.Contracts;
using ShopArticles.Core.Models;
using ShopArticles.Infrastructure.InMemory;

namespace ShopArticles.UnitTests;

public class InMemoryArticleRepositoryTests
{
    private readonly InMemoryArticleRepository _repository = new();

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
}
