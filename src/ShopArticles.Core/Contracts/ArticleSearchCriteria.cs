namespace ShopArticles.Core.Contracts;

// Filters for listing articles. All filters are optional and combined with AND.
// Name: partial, case-insensitive match on the article name.
// Category: exact match on the complete category name.
public record ArticleSearchCriteria(string? Name = null, string? Category = null);
