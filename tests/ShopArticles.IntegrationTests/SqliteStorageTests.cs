using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace ShopArticles.IntegrationTests;

// Runs the app with Storage:Provider=Sqlite and proves that articles survive an app restart.
public class SqliteStorageTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"articles-it-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Articles_survive_an_app_restart()
    {
        await using (var firstRun = CreateApp())
        {
            var client = firstRun.CreateClient();
            var payload = """{ "name": "Branded Memory Stick", "description": "Branded 16 GB memory stick", "price": 17.89, "currency": "NOK" }""";
            var response = await client.PostAsync("/api/articles", new StringContent(payload, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.True(File.Exists(_databasePath), "The app should have created the SQLite database file.");

        await using var secondRun = CreateApp();
        var stored = await secondRun.CreateClient().GetFromJsonAsync<JsonElement>("/api/articles/1");

        Assert.Equal("Branded Memory Stick", stored.GetProperty("name").GetString());
        Assert.Equal(17.89m, stored.GetProperty("price").GetDecimal());
    }

    private WebApplicationFactory<Program> CreateApp() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("Storage:Provider", "Sqlite")
            .UseSetting("ConnectionStrings:Articles", $"Data Source={_databasePath}"));

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            File.Delete(_databasePath + suffix);
    }
}
