# Shop Articles API

A RESTful back-end in ASP.NET Core (.NET 10) for registering and retrieving shop articles.

## Run

```bash
dotnet run --project src/ShopArticles.Api --urls http://localhost:5099
```

Swagger UI: <http://localhost:5099/swagger> (the root URL redirects there). The raw spec is at `/openapi/v1.yaml`.

## Test

```bash
dotnet test
```

- **Unit tests** (`tests/ShopArticles.UnitTests`): validation rules, the service, and the in-memory repository (search semantics, unique ids under concurrent inserts).
- **Integration tests** (`tests/ShopArticles.IntegrationTests`): run the whole app in memory with `WebApplicationFactory` and replay the examples from the assignment over HTTP.

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
  ShopArticles.Infrastructure  Repository implementations (in-memory for now) and their DI registration
tests/
  ShopArticles.UnitTests
  ShopArticles.IntegrationTests
```

Dependencies point inwards: `Api → Core ← Infrastructure`. Core has no ASP.NET dependency, so its logic can be unit tested in isolation. The repository is chosen through the `Storage:Provider` setting, so a database-backed implementation can be added without touching Core or Api.

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
- **Storage** is a thread-safe in-memory store (`ConcurrentDictionary` plus an atomic id counter) registered as a singleton. Data is not kept between runs, as the assignment allows.

## Plan and next steps

See [PLAN.md](PLAN.md).

## Tech

ASP.NET Core 10 (controllers), FluentValidation, OpenAPI 3 (hand-written) with Swagger UI, xUnit, Microsoft.AspNetCore.Mvc.Testing.
