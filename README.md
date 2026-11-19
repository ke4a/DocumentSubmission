# Country Document Submission

Backend for a two-step document workflow: an **Editor** uploads a document and submits it to a **Reviewer** in the same country, who approves it or returns it with a comment; after approval the Editor records the **recipient** it was distributed to. Every step is audited, and every uploaded version is kept.

The full specification is in [`docs/SPEC.md`](docs/SPEC.md).

## Run

Requires Docker.

```bash
docker compose up --build
```

This starts PostgreSQL and the API in the `Development` environment. The API waits for the database healthcheck, then applies the EF Core migrations and seed data.

- API: <http://localhost:8080>
- Interactive API docs (Scalar): <http://localhost:8080/scalar>
- OpenAPI document: <http://localhost:8080/openapi/v1.json>

Stop and delete the data with `docker compose down -v`.

To run the API from the IDE instead (`dotnet run --project DocumentSubmission.Api`), start only the database with `docker compose up db`. The API then listens on <http://localhost:5213>.

For local runs, Docker Compose and `appsettings.Development.json` provide all configuration. Elsewhere, set `ConnectionStrings__Database` and `Jwt__SigningKey` (at least 32 characters); the other `Jwt__*` settings have defaults in `appsettings.json`.

## Test

```bash
dotnet test
```

- `DocumentSubmission.Domain.Tests`: unit tests for the workflow rules. They need nothing else.
- `DocumentSubmission.Api.IntegrationTests`: HTTP and database tests. **They need Docker running**, because Testcontainers starts a PostgreSQL container for them.

## Seeded users and recipients

All data is fictional. Log in with a user Id (`POST /auth/login`, Development only).

| User Id | Name | Role | Country |
|---------|------|------|---------|
| `10000000-0000-0000-0000-000000000001` | Alice Ashford | Editor | NL |
| `10000000-0000-0000-0000-000000000002` | Bram Brink | Editor | NL |
| `10000000-0000-0000-0000-000000000003` | Clara Cole | Reviewer | NL |
| `10000000-0000-0000-0000-000000000004` | Daan Dekker | Reviewer | NL |
| `10000000-0000-0000-0000-000000000005` | Emma Evers | Editor | BE |
| `10000000-0000-0000-0000-000000000006` | Finn Fontaine | Reviewer | BE |

| Recipient Id | Name | Country |
|--------------|------|---------|
| `20000000-0000-0000-0000-000000000001` | Example Records Office | NL |
| `20000000-0000-0000-0000-000000000002` | Sample Archive Agency | NL |
| `20000000-0000-0000-0000-000000000003` | Fictional Registry Service | BE |

## Example flow

Two `.http` files walk through the whole workflow (login → create → return → resubmit → approve → distribute → audit) using the documents in `samples/`. There is one file per editor, because the editors pass values between requests differently:

- [`DocumentSubmission.Api.http`](DocumentSubmission.Api/DocumentSubmission.Api.http) for Visual Studio 2022 17.12+ or VS Code with the REST Client extension.
- [`DocumentSubmission.Api.Rider.http`](DocumentSubmission.Api/DocumentSubmission.Api.Rider.http) for JetBrains Rider or IntelliJ.

Alternatively, use the interactive docs at <http://localhost:8080/scalar>.

## Solution layout

| Project | Contains |
|---------|----------|
| `DocumentSubmission.Domain` | `Submission` aggregate (all workflow rules), versions, audit entries, users, domain exceptions. No framework dependencies. |
| `DocumentSubmission.Infrastructure` | `AppDbContext`, EF Core configuration, migrations, seed data |
| `DocumentSubmission.Api` | Controllers, request/response contracts, JWT auth and policies, upload validation, exception → ProblemDetails mapping, thin use-case services |
| `DocumentSubmission.Domain.Tests` | Unit tests, one class per `Submission` method |
| `DocumentSubmission.Api.IntegrationTests` | `WebApplicationFactory` + Testcontainers PostgreSQL tests |

## Summary of the specification

Condensed from [Assumptions](docs/SPEC.md#6-assumptions), [Decisions & trade-offs](docs/SPEC.md#7-decisions--trade-offs) and [API](docs/SPEC.md#8-api) in the full specification.

### Assumptions

- Users are seeded, not managed through the API; each has one role (Editor or Reviewer) and one country.
- A submission is visible only to its creating Editor and its assigned Reviewer. Other users get `404`, the same as for an unknown id.
- Creating a submission submits it immediately (no drafts). The Reviewer is fixed at creation; resubmissions go back to the same Reviewer.
- Only the creating Editor resubmits and distributes. A returned submission can be resubmitted any number of times.
- Recipients are seeded per country; distribution only records the recipient.
- Allowed documents are PDF or DOCX, at most 10 MB. For DOCX the signature check only confirms a ZIP container.
- Login exists only in Development.
- All timestamps are UTC.

### Decisions and trade-offs

- **Workflow rules in the `Submission` entity**: every state change goes through one `Transition()` method, which also writes the audit entry and increments `ConcurrencyToken`. Unit-tested without HTTP or a database. *Trade-off: the entity carries behaviour, not just data.*
- **Thin services using `AppDbContext` directly** (no repositories, no mediator). *Trade-off: use cases depend on EF Core.*
- **Rule violations throw typed exceptions** (not found, not allowed, invalid input, invalid state), which one handler turns into 404/403/400/409 ProblemDetails responses. *Trade-off: a method's signature doesn't show that it can fail; a result type would make that explicit, at the cost of extra code in every caller.*
- **Optimistic concurrency** with an explicit `ConcurrencyToken` column. A concurrent change, or two simultaneous resubmits, returns `409`. *Trade-off: the client must reload and retry.*
- **Approve and return name the `versionNumber`**, so a Reviewer only decides on the version they actually saw.
- **File bytes in a separate database table**: same transaction as the version, never loaded by list or detail queries. *Trade-off: doesn't suit large files; blob storage would be the production choice.*
- **Development-only JWT login** in place of an identity provider. *Trade-off: not production authentication.*
- **Integration tests on real PostgreSQL** (Testcontainers). *Trade-off: Docker is needed to run them.*

### API

All endpoints except login need `Authorization: Bearer <token>`. Errors are `application/problem+json`. Checks run in this order: `401` unauthenticated → `403` wrong role → `415`/`400` invalid request → `404` not visible → `400` ineligible reviewer/recipient → `409` wrong state, stale version or concurrent change.

| Method | Path | Who | Body |
|--------|------|-----|------|
| POST | `/auth/login` | anyone (Development only) | `{ "userId" }` → `{ token, expiresAt }` |
| GET | `/reviewers` | Editor | Reviewers in the caller's country |
| GET | `/recipients` | Editor | Recipients in the caller's country |
| GET | `/submissions` | Editor, Reviewer | Own / assigned submissions, newest first |
| POST | `/submissions` | Editor | multipart: `title`, `reviewerId`, `file`, optional `comment` → `201` + `Location` |
| GET | `/submissions/{id}` | participants | Details with all version metadata |
| POST | `/submissions/{id}/resubmit` | creating Editor | multipart: `file`, optional `comment` |
| POST | `/submissions/{id}/approve` | assigned Reviewer | `{ "versionNumber" }` |
| POST | `/submissions/{id}/return` | assigned Reviewer | `{ "versionNumber", "comment" }` |
| POST | `/submissions/{id}/distribute` | creating Editor | `{ "recipientId" }` |
| GET | `/submissions/{id}/versions/{versionNumber}/content` | participants | Downloads the original file |
| GET | `/submissions/{id}/audit` | participants | Audit trail, oldest first |

Workflow actions return `200` with the updated submission (same shape as `GET /submissions/{id}`).
