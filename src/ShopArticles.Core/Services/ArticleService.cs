using FluentValidation;
using FluentValidation.Results;
using ShopArticles.Core.Contracts;
using ShopArticles.Core.Models;
using ShopArticles.Core.Repositories;

namespace ShopArticles.Core.Services;

public class ArticleService(
    IArticleRepository repository,
    IValidator<CreateArticleRequest> validator,
    IValidator<UpdateArticleRequest> updateValidator) : IArticleService
{
    public async Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var created = await repository.AddAsync(ToModel(request), cancellationToken);
        return ArticleResponse.FromModel(created);
    }

    public async Task<IReadOnlyList<ArticleResponse>> CreateManyAsync(IReadOnlyList<CreateArticleRequest> requests, CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
            throw new ValidationException([new ValidationFailure("", "At least one article is required.")]);

        // Errors are keyed by position, e.g. "[1].Currency". The validator has no async rules, so Validate is enough.
        var failures = requests
            .SelectMany((request, i) => validator.Validate(request).Errors
                .Select(f => new ValidationFailure($"[{i}].{f.PropertyName}", f.ErrorMessage)))
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        // Real parallel inserts, so this relies on the repository being thread-safe.
        // Each task writes only its own slot, which keeps the results in request order.
        var created = new ArticleResponse[requests.Count];
        await Parallel.ForEachAsync(Enumerable.Range(0, requests.Count), cancellationToken, async (i, token) =>
        {
            var article = await repository.AddAsync(ToModel(requests[i]), token);
            created[i] = ArticleResponse.FromModel(article);
        });

        return created;
    }

    public async Task<UpdateResult<ArticleResponse>> UpdateAsync(long id, UpdateArticleRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var article = ToModel(request);
        article.Id = id;

        var result = await repository.UpdateAsync(article, request.Version!.Value, cancellationToken);
        return new UpdateResult<ArticleResponse>(result.Status, result.Value is null ? null : ArticleResponse.FromModel(result.Value));
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

    private static Article ToModel(CreateArticleRequest request) => new()
    {
        Name = request.Name!,
        Description = request.Description!,
        Category = string.IsNullOrEmpty(request.Category) ? null : request.Category,
        Price = request.Price!.Value,
        Currency = string.IsNullOrEmpty(request.Currency) ? null : request.Currency.ToUpperInvariant(),
    };
}
