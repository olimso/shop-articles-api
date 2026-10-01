using ShopArticles.Core.Contracts;
using ShopArticles.Core.Models;

namespace ShopArticles.Core.Repositories;

public interface IArticleRepository
{
    // Stores the article, assigns its Id and returns it.
    Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default);

    Task<Article?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Article>> SearchAsync(ArticleSearchCriteria criteria, CancellationToken cancellationToken = default);
}
