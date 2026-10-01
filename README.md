# Shop Articles API

A RESTful back-end in ASP.NET Core (.NET 10) for registering and retrieving shop articles.

## Run

```bash
dotnet run --project src/ShopArticles.Api --urls http://localhost:5099
```

Swagger UI: <http://localhost:5099/swagger> (the root URL redirects there). The raw spec is at `/openapi/v1.yaml`.

### Storage

Articles are kept in memory by default and are lost when the app stops. To keep them in a SQLite database instead:

```bash
dotnet run --project src/ShopArticles.Api --urls http://localhost:5099 \
  --Storage:Provider=Sqlite --ConnectionStrings:Articles="Data Source=articles.db"
```

The same settings can go in `appsettings.json`. On startup the app creates the database file and applies any pending EF Core migrations.

To change the schema, edit `ArticlesDbContext` and add a migration with the repo's local `dotnet ef` tool:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/ShopArticles.Infrastructure --startup-project src/ShopArticles.Api --output-dir Persistence/Migrations
```

## Test

```bash
dotnet test
```

- **Unit tests** (`tests/ShopArticles.UnitTests`): validation rules, the service, and the repository. The repository tests run against both the in-memory store and a real SQLite file (search semantics, unique ids under concurrent inserts, exact price round-trip).
- **Integration tests** (`tests/ShopArticles.IntegrationTests`): run the whole app in memory with `WebApplicationFactory` and replay the examples from the assignment over HTTP. One test runs the app on SQLite and checks that articles survive a restart.

## Endpoints

| Method | URL | Result |
|---|---|---|
| `POST` | `/api/articles` | `200` with the created article and its generated `article_id`; `400` on invalid input |
| `POST` | `/api/articles-concurrent` | Array of articles, inserted in parallel. `200` with the created articles in request order; `400` (and nothing stored) if any article is invalid |
| `GET` | `/api/articles/{id}` | `200` with the article; `404` if it doesn't exist |
| `GET` | `/api/articles?name=&category=` | `200` with an array of matches (`[]` if none) |

### Article

| Property | Required | Format |
|---|---|---|
| `article_id` | – | Number > 0. Must not be sent in POST; always present in responses |
| `name` | yes | String ≤ 64 characters |
| `description` | yes | String ≤ 2048 characters |
| `category` | no | String ≤ 64 characters |
| `price` | yes | JSON number ≥ 0 |
| `currency` | no | ISO 4217 code; may be blank only when `price` is 0 |

```bash
curl -X POST http://localhost:5099/api/articles -H 'Content-Type: application/json' -d '{
  "name": "Branded Memory Stick",
  "description": "Branded 16 GB memory stick",
  "category": "USB flash drive",
  "price": 17.89,
  "currency": "NOK"
}'
curl -X POST http://localhost:5099/api/articles-concurrent -H 'Content-Type: application/json' -d '[
  { "name": "Branded Drinking Mug", "description": "Porcelain drinking cup with your logo on it.", "category": "Mug", "price": 0 },
  { "name": "Branded Pen", "description": "Blue ballpoint pen", "price": 2.5, "currency": "EUR" }
]'
curl http://localhost:5099/api/articles/1
curl 'http://localhost:5099/api/articles?name=branded'
curl 'http://localhost:5099/api/articles?category=USB%20flash%20drive'
```

## Architecture

```
src/
  ShopArticles.Api             HTTP layer: controller, JSON settings, error handling, openapi.yaml
  ShopArticles.Core            Domain model, request/response contracts, validation, service, repository interface
  ShopArticles.Infrastructure  Repository implementations (in-memory and EF Core + SQLite), migrations, DI registration
tests/
  ShopArticles.UnitTests
  ShopArticles.IntegrationTests
```

Dependencies point inwards: `Api → Core ← Infrastructure`. Core has no ASP.NET dependency, so its logic can be unit tested in isolation. The repository is chosen through the `Storage:Provider` setting (`InMemory` or `Sqlite`); adding SQLite did not change Core, and Api only gained the startup migration call.

## Design decisions and assumptions

- **The API is documented design-first.** [`src/ShopArticles.Api/openapi.yaml`](src/ShopArticles.Api/openapi.yaml) is a hand-written OpenAPI 3 spec and the source of truth for the HTTP contract; it is not generated from the code. Swagger UI only displays it. A change to an endpoint must be made in the spec as well.
- **POST returns `200 OK`**, not `201 Created`, because that is what the assignment specifies.
- **`price` must be a JSON number.** ASP.NET Core accepts numbers sent as strings by default; strict number handling is turned on so `"17.89"` gives `400`.
- **`article_id` in a POST body gives `400`**, instead of being silently ignored.
- **Name search** is a case-insensitive *contains* match. **Category search** is an exact, case-sensitive match on the complete name. Both filters combine with AND; no filters returns all articles.
- **Currency** is checked against a fixed list of active ISO 4217 codes, case-insensitively, and stored upper-case. It is required when `price > 0`.
- **Null fields are left out** of responses (e.g. `currency` on a free article), matching the assignment's examples.
- **Errors** use RFC 7807 problem details. Validation errors are keyed by the JSON field name (`price`, `article_id`).
- **Concurrent insert is all-or-nothing on validation.** Every article is validated before any is stored, so one invalid article gives `400` and stores nothing. Errors are keyed by position, e.g. `[1].currency`. Valid articles are then inserted with `Parallel.ForEachAsync`; ids are unique but need not follow the request order. An empty array gives `400`.
- **Storage** is in memory by default: a thread-safe `ConcurrentDictionary` plus an atomic id counter, registered as a singleton. Data is not kept between runs.
- **SQLite storage** uses EF Core. Each repository call creates its own `DbContext` through `IDbContextFactory`, because a `DbContext` is not thread-safe and the concurrent insert calls the repository in parallel. WAL mode is turned on so reads are not blocked while a write is in progress.
- **Schema changes go through EF Core migrations**, applied automatically on startup, so an existing database is upgraded instead of recreated.
- **Prices are stored in SQLite as exact text** (`"17.89"`), so a price of `0` comes back as `0` and not `0.0`, the same as the in-memory store.
- **Name search in SQLite** uses `LIKE`, which ignores case only for ASCII letters (`ø` does not match `Ø`). `%` and `_` in the search text are matched literally.

## Plan and next steps

See [PLAN.md](PLAN.md).

## Tech

ASP.NET Core 10 (controllers), EF Core 10 + SQLite, FluentValidation, OpenAPI 3 (hand-written) with Swagger UI, xUnit, Microsoft.AspNetCore.Mvc.Testing.
