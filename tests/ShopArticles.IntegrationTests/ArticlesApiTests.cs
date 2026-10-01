using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ShopArticles.IntegrationTests;

// Runs the real application in memory and replays the examples from the assignment.
// Each test gets its own app instance, so the in-memory store starts empty and ids start at 1.
public class ArticlesApiTests : IAsyncLifetime
{
    private const string MemoryStick = """
        {
            "name": "Branded Memory Stick",
            "description": "Branded 16 GB memory stick",
            "category": "USB flash drive",
            "price": 17.89,
            "currency": "NOK"
        }
        """;

    private const string DrinkingMug = """
        {
            "name": "Branded Drinking Mug",
            "description": "Porcelain drinking cup with your logo on it.",
            "category": "Mug",
            "price": 0
        }
        """;

    private readonly WebApplicationFactory<Program> _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Post_returns_article_with_generated_id()
    {
        var response = await PostJson(MemoryStick);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.Equal(1, body.GetProperty("article_id").GetInt64());
        Assert.Equal("Branded Memory Stick", body.GetProperty("name").GetString());
        Assert.Equal(17.89m, body.GetProperty("price").GetDecimal());
        Assert.Equal("NOK", body.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task Post_free_article_omits_currency_from_response()
    {
        var response = await PostJson(DrinkingMug);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.False(body.TryGetProperty("currency", out _));
    }

    [Fact]
    public async Task Post_accepts_trailing_comma_as_in_the_assignment_example()
    {
        var response = await PostJson("""
            {
                "name": "Branded Drinking Mug",
                "description": "Porcelain drinking cup with your logo on it.",
                "category": "Mug",
                "price": 0,
            }
            """);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("""{ "name": "Branded Memory Stick", "description": "Branded 16 GB memory stick", "category": "USB flash drive", "price": 17.89 }""")]
    [InlineData("""{ "name": "Branded Memory Stick", "description": "Branded 16 GB memory stick", "category": "USB flash drive", "price": 17.89, }""")]
    [InlineData("""{ "name": "Branded Memory Stick", "description": "Branded 16 GB memory stick", "category": "USB flash drive", "price": "17.89", "currency": "NOK" }""")]
    [InlineData("""{ "article_id": 7, "name": "Branded Memory Stick", "description": "Branded 16 GB memory stick", "price": 0 }""")]
    [InlineData("""{ "description": "Missing name", "price": 0 }""")]
    [InlineData("""not json""")]
    public async Task Post_invalid_payload_returns_400(string payload)
    {
        var response = await PostJson(payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_concurrent_stores_all_articles()
    {
        var response = await PostJson($"[{MemoryStick}, {DrinkingMug}]", "/api/articles-concurrent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await ReadJson(response)).EnumerateArray().ToList();
        Assert.Equal(new[] { "Branded Memory Stick", "Branded Drinking Mug" }, body.Select(a => a.GetProperty("name").GetString()));
        Assert.Equal(new long[] { 1, 2 }, body.Select(a => a.GetProperty("article_id").GetInt64()).Order());

        var stored = await ReadJson(await _client.GetAsync("/api/articles"));
        Assert.Equal(2, stored.GetArrayLength());
    }

    [Fact]
    public async Task Post_concurrent_with_one_invalid_article_returns_400_and_stores_nothing()
    {
        const string missingCurrency = """{ "name": "Paid", "description": "No currency", "price": 5 }""";

        var response = await PostJson($"[{MemoryStick}, {missingCurrency}]", "/api/articles-concurrent");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJson(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("[1].currency", out _), errors.ToString());

        var stored = await ReadJson(await _client.GetAsync("/api/articles"));
        Assert.Equal(0, stored.GetArrayLength());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData(MemoryStick)]
    public async Task Post_concurrent_requires_a_non_empty_array(string payload)
    {
        var response = await PostJson(payload, "/api/articles-concurrent");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_returns_version_1()
    {
        var body = await ReadJson(await PostJson(MemoryStick));

        Assert.Equal(1, body.GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task Put_with_current_version_updates_article()
    {
        await PostJson(MemoryStick);

        var response = await PutJson("/api/articles/1", UpdatedMemoryStick(version: 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.Equal(2, body.GetProperty("version").GetInt64());
        Assert.Equal(31.89m, body.GetProperty("price").GetDecimal());

        var stored = await ReadJson(await _client.GetAsync("/api/articles/1"));
        Assert.Equal("Branded 256 GB memory stick", stored.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Put_after_another_users_update_returns_409()
    {
        await PostJson(MemoryStick);
        // Two users loaded version 1. The first one saves...
        await PutJson("/api/articles/1", UpdatedMemoryStick(version: 1));

        // ...so the second one's save, still based on version 1, must be rejected.
        var response = await PutJson("/api/articles/1", UpdatedMemoryStick(version: 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var stored = await ReadJson(await _client.GetAsync("/api/articles/1"));
        Assert.Equal(2, stored.GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task Put_of_unknown_article_returns_404()
    {
        var response = await PutJson("/api/articles/99", UpdatedMemoryStick(version: 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    // The assignment's PUT example: price as a string and no version. Rejected, consistent with POST.
    [InlineData("""{ "name": "Branded Memory Stick", "description": "Branded 256 GB memory stick", "category": "USB flash drive", "price": "31.89", "currency": "NOK" }""")]
    [InlineData("""{ "name": "Branded Memory Stick", "description": "Branded 256 GB memory stick", "category": "USB flash drive", "price": 31.89, "currency": "NOK" }""")]
    [InlineData("""{ "article_id": 1, "name": "Branded Memory Stick", "description": "Branded 256 GB memory stick", "price": 31.89, "currency": "NOK", "version": 1 }""")]
    public async Task Put_invalid_payload_returns_400(string payload)
    {
        await PostJson(MemoryStick);

        var response = await PutJson("/api/articles/1", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_returns_stored_article()
    {
        await PostJson(MemoryStick);

        var response = await _client.GetAsync("/api/articles/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.Equal(1, body.GetProperty("article_id").GetInt64());
        Assert.Equal("USB flash drive", body.GetProperty("category").GetString());
    }

    [Fact]
    public async Task Get_by_unknown_id_returns_404()
    {
        await PostJson(MemoryStick);
        await PostJson(DrinkingMug);

        var response = await _client.GetAsync("/api/articles/3");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/articles?category=USB%20flash%20drive", new long[] { 1 })]
    [InlineData("/api/articles?name=Branded", new long[] { 1, 2 })]
    [InlineData("/api/articles?name=bRaNdEd", new long[] { 1, 2 })]
    [InlineData("/api/articles?name=Branded&category=Mug", new long[] { 2 })]
    [InlineData("/api/articles", new long[] { 1, 2 })]
    [InlineData("/api/articles?name=256%20GB", new long[0])]
    [InlineData("/api/articles?category=USB", new long[0])]
    public async Task Search_returns_matching_articles(string url, long[] expectedIds)
    {
        await PostJson(MemoryStick);
        await PostJson(DrinkingMug);

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(expectedIds, body.EnumerateArray().Select(a => a.GetProperty("article_id").GetInt64()));
    }

    [Fact]
    public async Task OpenApi_document_is_served()
    {
        var response = await _client.GetAsync("/openapi/v1.yaml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var spec = await response.Content.ReadAsStringAsync();
        Assert.Contains("  /api/articles:", spec);
        Assert.Contains("  /api/articles/{id}:", spec);
    }

    [Fact]
    public async Task Swagger_ui_is_served()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string UpdatedMemoryStick(long version) => $$"""
        {
            "name": "Branded Memory Stick",
            "description": "Branded 256 GB memory stick",
            "category": "USB flash drive",
            "price": 31.89,
            "currency": "NOK",
            "version": {{version}}
        }
        """;

    private Task<HttpResponseMessage> PutJson(string url, string json) =>
        _client.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

    private Task<HttpResponseMessage> PostJson(string json, string url = "/api/articles") =>
        _client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
