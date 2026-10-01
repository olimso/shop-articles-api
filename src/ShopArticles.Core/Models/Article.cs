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
}
