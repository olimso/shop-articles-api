using System.Collections.Concurrent;
using ShopArticles.Core.Contracts;
using ShopArticles.Core.Models;
using ShopArticles.Core.Repositories;

namespace ShopArticles.Infrastructure.InMemory;

// Thread-safe, non-persistent article store. Ids come from an atomic counter, so
// concurrent inserts never get the same id.
public class InMemoryArticleRepository : IArticleRepository
{
    private readonly ConcurrentDictionary<long, Article> _articles = new();
    private long _lastId;

    public Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default)
    {
        var stored = Copy(article);
        stored.Id = Interlocked.Increment(ref _lastId);
        _articles[stored.Id] = stored;
        return Task.FromResult(Copy(stored));
    }

    public Task<Article?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var article = _articles.TryGetValue(id, out var found) ? Copy(found) : null;
        return Task.FromResult(article);
    }

    public Task<IReadOnlyList<Article>> SearchAsync(ArticleSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        IEnumerable<Article> query = _articles.Values;

        if (!string.IsNullOrEmpty(criteria.Name))
            query = query.Where(a => a.Name.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(criteria.Category))
            query = query.Where(a => string.Equals(a.Category, criteria.Category, StringComparison.Ordinal));

        IReadOnlyList<Article> result = query.OrderBy(a => a.Id).Select(Copy).ToList();
        return Task.FromResult(result);
    }

    // Callers get copies so they can't mutate the stored state behind the repository's back.
    private static Article Copy(Article a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Description = a.Description,
        Category = a.Category,
        Price = a.Price,
        Currency = a.Currency,
    };
}
