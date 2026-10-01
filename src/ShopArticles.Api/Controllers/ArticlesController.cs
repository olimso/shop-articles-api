using Microsoft.AspNetCore.Mvc;
using ShopArticles.Core.Contracts;
using ShopArticles.Core.Repositories;
using ShopArticles.Core.Services;

namespace ShopArticles.Api.Controllers;

// The HTTP contract for these endpoints is documented in openapi.yaml.
[ApiController]
[Route("api/articles")]
[Produces("application/json")]
public class ArticlesController(IArticleService articleService) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult<ArticleResponse>> Create(CreateArticleRequest request, CancellationToken cancellationToken)
    {
        var created = await articleService.CreateAsync(request, cancellationToken);
        return Ok(created);
    }

    // The leading slash makes this route absolute, so it is not prefixed with "api/articles".
    [HttpPost("/api/articles-concurrent")]
    [Consumes("application/json")]
    public async Task<ActionResult<IReadOnlyList<ArticleResponse>>> CreateMany(List<CreateArticleRequest> requests, CancellationToken cancellationToken)
    {
        var created = await articleService.CreateManyAsync(requests, cancellationToken);
        return Ok(created);
    }

    [HttpPut("{id:long}")]
    [Consumes("application/json")]
    public async Task<ActionResult<ArticleResponse>> Update(long id, UpdateArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await articleService.UpdateAsync(id, request, cancellationToken);

        return result.Status switch
        {
            UpdateStatus.Updated => Ok(result.Value),
            UpdateStatus.NotFound => NotFound(),
            UpdateStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The article was changed by someone else.",
                detail: "Load the article again to get its current version, then retry the update."),
            _ => throw new InvalidOperationException($"Unexpected update status {result.Status}."),
        };
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ArticleResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var article = await articleService.GetByIdAsync(id, cancellationToken);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ArticleResponse>>> Search(
        [FromQuery] string? name,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var articles = await articleService.SearchAsync(new ArticleSearchCriteria(name, category), cancellationToken);
        return Ok(articles);
    }
}
