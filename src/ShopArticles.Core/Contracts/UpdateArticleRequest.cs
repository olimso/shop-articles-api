namespace ShopArticles.Core.Contracts;

// Payload for updating an article: the same fields and rules as creating one, plus the version the
// client last saw. The rules are in UpdateArticleRequestValidator.
public class UpdateArticleRequest : CreateArticleRequest
{
    public long? Version { get; set; }
}
