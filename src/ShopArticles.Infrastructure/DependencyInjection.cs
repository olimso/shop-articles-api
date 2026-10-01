using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopArticles.Core.Repositories;
using ShopArticles.Infrastructure.InMemory;
using ShopArticles.Infrastructure.Persistence;

namespace ShopArticles.Infrastructure;

public static class DependencyInjection
{
    // Registers the article repository selected by the Storage:Provider setting:
    // InMemory (the default) or Sqlite, which uses the "Articles" connection string.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // A blank value (e.g. "Storage__Provider=" in the environment) counts as unset.
        var provider = configuration["Storage:Provider"]?.Trim();
        if (string.IsNullOrEmpty(provider))
            provider = "InMemory";

        switch (provider.ToLowerInvariant())
        {
            case "inmemory":
                // Singleton: the in-memory store must live for the lifetime of the app.
                services.AddSingleton<IArticleRepository, InMemoryArticleRepository>();
                break;
            case "sqlite":
                var connectionString = configuration.GetConnectionString("Articles")
                    ?? throw new InvalidOperationException("Storage:Provider is Sqlite, but ConnectionStrings:Articles is not set.");
                services.AddDbContextFactory<ArticlesDbContext>(options => options.UseSqlite(connectionString));
                services.AddSingleton<IArticleRepository, EfArticleRepository>();
                break;
            default:
                throw new InvalidOperationException($"Unknown storage provider '{provider}'.");
        }

        return services;
    }

    // Brings the database schema up to date by applying pending migrations. Does nothing for the in-memory store.
    public static async Task InitializeStorageAsync(this IServiceProvider services)
    {
        var contextFactory = services.GetService<IDbContextFactory<ArticlesDbContext>>();
        if (contextFactory is null)
            return;

        await using var db = await contextFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        // WAL lets reads continue while a write is in progress, which helps the concurrent insert.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    }
}
