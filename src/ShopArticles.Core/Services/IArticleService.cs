using ShopArticles.Core.Contracts;

namespace ShopArticles.Core.Services;

public interface IArticleService
{
    // Validates and stores a new article.
    // Throws a FluentValidation ValidationException when the request breaks the article contract.
    Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken cancellationToken = default);

    // Validates all requests first, then stores them concurrently. Nothing is stored if any request is invalid.
    // Throws a FluentValidation ValidationException with errors keyed by item index, e.g. "[1].Price".
    // Results are returned in the order of the requests.
    Task<IReadOnlyList<ArticleResponse>> CreateManyAsync(IReadOnlyList<CreateArticleRequest> requests, CancellationToken cancellationToken = default);

    Task<ArticleResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticleResponse>> SearchAsync(ArticleSearchCriteria criteria, CancellationToken cancellationToken = default);
}
