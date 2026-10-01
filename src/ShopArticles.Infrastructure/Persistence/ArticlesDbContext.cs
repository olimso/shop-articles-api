using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ShopArticles.Core.Models;
using ShopArticles.Core.Validation;

namespace ShopArticles.Infrastructure.Persistence;

public class ArticlesDbContext(DbContextOptions<ArticlesDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(article =>
        {
            article.ToTable("articles");
            article.HasKey(a => a.Id);
            article.Property(a => a.Id).HasColumnName("article_id").ValueGeneratedOnAdd();
            article.Property(a => a.Name).HasColumnName("name").HasMaxLength(CreateArticleRequestValidator.NameMaxLength);
            article.Property(a => a.Description).HasColumnName("description").HasMaxLength(CreateArticleRequestValidator.DescriptionMaxLength);
            article.Property(a => a.Category).HasColumnName("category").HasMaxLength(CreateArticleRequestValidator.CategoryMaxLength);
            // EF stores decimals as TEXT in SQLite but always with a decimal point, so 0 would come back as 0.0
            // (and be serialized as 0.0). Storing the exact string keeps the value as the client sent it.
            article.Property(a => a.Price).HasColumnName("price").HasConversion(
                v => v.ToString(CultureInfo.InvariantCulture),
                v => decimal.Parse(v, CultureInfo.InvariantCulture));
            article.Property(a => a.Currency).HasColumnName("currency").HasMaxLength(3);
            // A concurrency token: EF adds "AND version = <original value>" to every UPDATE of this row.
            article.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();

            // Category search is an exact match, so an index makes it a lookup instead of a table scan.
            article.HasIndex(a => a.Category);
        });
    }
}
