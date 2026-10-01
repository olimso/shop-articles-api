using ShopArticles.Core.Contracts;
using ShopArticles.Core.Models;

namespace ShopArticles.Core.Repositories;

public interface IArticleRepository
{
    // Stores the article, assigns its Id and returns it.
    Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default);

    // Replaces the stored article with the same Id, but only if its Version still equals expectedVersion.
    // On success the stored Version goes up by one. The check and the write happen as one atomic step.
    Task<UpdateResult<Article>> UpdateAsync(Article article, long expectedVersion, CancellationToken cancellationToken = default);

    Task<Article?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Article>> SearchAsync(ArticleSearchCriteria criteria, CancellationToken cancellationToken = default);
}
