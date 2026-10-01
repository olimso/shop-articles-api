using ShopArticles.Core.Contracts;

namespace ShopArticles.Core.Services;

public interface IArticleService
{
    // Validates and stores a new article.
    // Throws a FluentValidation ValidationException when the request breaks the article contract.
    Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken cancellationToken = default);

    Task<ArticleResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticleResponse>> SearchAsync(ArticleSearchCriteria criteria, CancellationToken cancellationToken = default);
}
