using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopArticles.Core.Repositories;
using ShopArticles.Infrastructure.InMemory;

namespace ShopArticles.Infrastructure;

public static class DependencyInjection
{
    // Registers the article repository selected by the Storage:Provider setting.
    // Only InMemory (the default) is supported for now.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Storage:Provider"] ?? "InMemory";

        switch (provider)
        {
            case "InMemory":
                // Singleton: the in-memory store must live for the lifetime of the app.
                services.AddSingleton<IArticleRepository, InMemoryArticleRepository>();
                break;
            default:
                throw new InvalidOperationException($"Unknown storage provider '{provider}'.");
        }

        return services;
    }
}
