using Microsoft.AspNetCore.Mvc;
using ShopArticles.Core.Contracts;
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
