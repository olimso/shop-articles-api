using FluentValidation;
using ShopArticles.Core.Contracts;
using ShopArticles.Core.Models;
using ShopArticles.Core.Repositories;

namespace ShopArticles.Core.Services;

public class ArticleService(IArticleRepository repository, IValidator<CreateArticleRequest> validator) : IArticleService
{
    public async Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var article = new Article
        {
            Name = request.Name!,
            Description = request.Description!,
            Category = string.IsNullOrEmpty(request.Category) ? null : request.Category,
            Price = request.Price!.Value,
            Currency = string.IsNullOrEmpty(request.Currency) ? null : request.Currency.ToUpperInvariant(),
        };

        var created = await repository.AddAsync(article, cancellationToken);
        return ArticleResponse.FromModel(created);
    }

    public async Task<ArticleResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var article = await repository.GetByIdAsync(id, cancellationToken);
        return article is null ? null : ArticleResponse.FromModel(article);
    }

    public async Task<IReadOnlyList<ArticleResponse>> SearchAsync(ArticleSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var articles = await repository.SearchAsync(criteria, cancellationToken);
        return articles.Select(ArticleResponse.FromModel).ToList();
    }
}
