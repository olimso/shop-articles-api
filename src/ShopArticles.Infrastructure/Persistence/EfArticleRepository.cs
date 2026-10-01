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
        stored.Version = 1;
        db.Articles.Add(stored);
        await db.SaveChangesAsync(cancellationToken);
        return stored;
    }

    public async Task<UpdateResult<Article>> UpdateAsync(Article article, long expectedVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Attach without loading: EF treats the attached values as the "original" ones, so Version is a
        // concurrency token with original value expectedVersion. SaveChanges then runs
        // UPDATE ... WHERE article_id = @id AND version = @expectedVersion, which is the check and the write in one.
        var updated = Copy(article);
        updated.Version = expectedVersion;
        db.Articles.Attach(updated).State = EntityState.Modified;
        updated.Version = expectedVersion + 1;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new UpdateResult<Article>(UpdateStatus.Updated, Copy(updated));
        }
        catch (DbUpdateConcurrencyException)
        {
            // No row matched: either the article does not exist or its version has moved on.
            var exists = await db.Articles.AnyAsync(a => a.Id == article.Id, cancellationToken);
            return new UpdateResult<Article>(exists ? UpdateStatus.Conflict : UpdateStatus.NotFound);
        }
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
        Version = a.Version,
    };
}
