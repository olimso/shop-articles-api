using ShopArticles.Core.Models;

namespace ShopArticles.Core.Contracts;

// An article as returned by the API.
public class ArticleResponse
{
    // Server-generated id.
    public long ArticleId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? Category { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }

    public static ArticleResponse FromModel(Article article) => new()
    {
        ArticleId = article.Id,
        Name = article.Name,
        Description = article.Description,
        Category = article.Category,
        Price = article.Price,
        Currency = article.Currency,
    };
}
