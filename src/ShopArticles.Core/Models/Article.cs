namespace ShopArticles.Core.Models;

// A shop article as stored by the repository.
public class Article
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }

    // Starts at 1 and goes up by one on every update. Used to detect that someone else changed the article.
    public long Version { get; set; }
}
