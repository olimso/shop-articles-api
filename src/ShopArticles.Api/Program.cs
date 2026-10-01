using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using ShopArticles.Api.ErrorHandling;
using ShopArticles.Core.Contracts;
using ShopArticles.Core.Services;
using ShopArticles.Core.Validation;
using ShopArticles.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // The contract uses snake_case (article_id) and leaves out empty fields (e.g. currency on a free article).
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // ASP.NET Core's web defaults accept numbers sent as strings; the contract requires "price": "17.89" to be a 400.
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.Services.AddSingleton<IValidator<CreateArticleRequest>, CreateArticleRequestValidator>();
builder.Services.AddSingleton<IValidator<UpdateArticleRequest>, UpdateArticleRequestValidator>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeStorageAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

// The API contract is written by hand in openapi.yaml (design-first); Swagger UI only displays it.
var openApiFile = Path.Combine(app.Environment.ContentRootPath, "openapi.yaml");
app.MapGet("/openapi/v1.yaml", () => Results.File(openApiFile, "application/yaml"));
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.yaml", "Shop Articles API v1"));
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
