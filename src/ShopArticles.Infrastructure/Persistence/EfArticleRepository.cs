using Microsoft.EntityFrameworkCore;
using ShopArticles.Core.Contracts;
using ShopArticles.Core.Models;
using ShopArticles.Core.Repositories;

namespace ShopArticles.Infrastructure.Persistence;

// A DbContext is not thread-safe, so every call creates its own from the factory.
// That keeps the concurrent insert endpoint safe: parallel AddAsync calls never share a context.
public class EfArticleRepository(IDbContextFactory<ArticlesDbContext> contextFactory) : IArticleRepository
{
    public async Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var stored = Copy(article);
        stored.Id = 0;
        db.Articles.Add(stored);
        await db.SaveChangesAsync(cancellationToken);
        return stored;
    }

    public async Task<Article?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Article>> SearchAsync(ArticleSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<Article> query = db.Articles.AsNoTracking();

        // SQLite's LIKE ignores case (for ASCII letters only); % and _ in the search text are escaped so they match literally.
        if (!string.IsNullOrEmpty(criteria.Name))
            query = query.Where(a => EF.Functions.Like(a.Name, $"%{EscapeLike(criteria.Name)}%", "\\"));

        // = in SQLite compares exactly, so this is a case-sensitive full match like the in-memory store.
        if (!string.IsNullOrEmpty(criteria.Category))
            query = query.Where(a => a.Category == criteria.Category);

        return await query.OrderBy(a => a.Id).ToListAsync(cancellationToken);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

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
