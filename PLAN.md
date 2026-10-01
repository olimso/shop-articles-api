# Plan

How the Shop Articles API was planned and built, and what comes next. For running and using the API, see the [README](README.md).

## Scope

A RESTful back-end in ASP.NET Core for registering and retrieving shop articles:

- `POST /api/articles`: create an article
- `GET /api/articles/{id}`: get one article
- `GET /api/articles?name=&category=`: search

Plus Swagger documentation, unit tests and integration tests. The architecture is shaped so the extras under [Next steps](#next-steps) fit in without rework.

## Contract

Articles are JSON in snake_case. The full contract is in [`src/ShopArticles.Api/openapi.yaml`](src/ShopArticles.Api/openapi.yaml).

| Field | Rule |
|---|---|
| `article_id` | Number > 0. Must not be sent in POST; always present in responses |
| `name` | Required, ≤ 64 characters |
| `description` | Required, ≤ 2048 characters |
| `category` | Optional, ≤ 64 characters |
| `price` | Required JSON number ≥ 0. A string such as `"17.89"` gives `400` |
| `currency` | ISO 4217 code. May be blank only when `price` is 0 |
| `version` | Set by the server (1 on create, +1 per update). Required in PUT |

- POST returns `200 OK` (as the assignment specifies), not `201 Created`. Invalid input gives `400`.
- GET by id returns `200`, or `404` when the id does not exist.
- PUT replaces the article if `version` still matches (`200`), otherwise `409 Conflict`. `404` for an unknown id.
- Search: `name` is a case-insensitive *contains* match, `category` an exact match. Filters combine with AND. No match gives `[]`.
- Null fields are left out of responses.

## Architecture

```
src/
  ShopArticles.Api             Controller, JSON settings, error handling, openapi.yaml
  ShopArticles.Core            Model, contracts, validation, service, repository interface (no ASP.NET dependency)
  ShopArticles.Infrastructure  In-memory and EF Core + SQLite repositories, migrations, DI registration
tests/
  ShopArticles.UnitTests         Validator, service, repository
  ShopArticles.IntegrationTests  Whole app in memory via WebApplicationFactory
```

References point inwards: `Api → Core ← Infrastructure`.

## Key decisions

- **JSON:** snake_case names, null fields omitted, and strict number handling so a string `price` is rejected.
- **Validation:** FluentValidation in the service. Rule errors are returned as `ValidationProblemDetails`, the same `400` shape ASP.NET Core uses for unreadable JSON.
- **Currencies:** a hardcoded list of active ISO 4217 codes, because `RegionInfo` gives different results on different operating systems.
- **Errors:** RFC 7807 problem details for `400` and `404`.
- **API docs:** design-first. The hand-written `openapi.yaml` is the source of truth and Swagger UI only displays it, so the code has no documentation attributes.
- **Updates:** optimistic concurrency with a `version` attribute. The version check and the write are one atomic step in both storages (`TryUpdate` in memory, an EF Core concurrency token in SQLite). A string `price` in PUT gives `400`, consistent with POST, although the assignment's PUT example sends one.
- **Storage:** the repository is an async interface chosen by the `Storage:Provider` setting: in memory (default) or EF Core + SQLite. The SQLite repository creates a `DbContext` per call so the concurrent insert stays thread-safe, and the schema is managed with migrations applied on startup.

## Status

| Step | Status |
|---|---|
| Solution and project setup | ✅ Done |
| Core: model, contracts, validator, currency codes, service | ✅ Done |
| Infrastructure: thread-safe in-memory repository | ✅ Done |
| Api: controller, JSON, error handling | ✅ Done |
| API docs: `openapi.yaml` + Swagger UI | ✅ Done |
| Unit tests | ✅ Done |
| Integration tests | ✅ Done |
| README | ✅ Done |
| Publish to GitHub | ✅ Done |
| Concurrent insert: `POST /api/articles-concurrent` | ✅ Done |
| Persistence: EF Core + SQLite, selected by `Storage:Provider` | ✅ Done |
| Update: `PUT /api/articles/{id}` with optimistic concurrency (`409`) | ✅ Done |

## Next steps

In order of priority:

1. **Contract check:** a test that compares `openapi.yaml` with the running API, so the hand-written spec cannot drift from the code.
