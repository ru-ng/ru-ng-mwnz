# Companies API

ASP.NET Core 9 minimal API that exposes `GET /companies/{id}`, loads company XML from a static upstream host, maps it to JSON, and returns structured errors when validation or upstream handling fails.

## Prerequisites

- **.NET 9 SDK** (for `dotnet run` / `dotnet test`), **or**
- **Docker** (for build/run without a local SDK)

## Run locally

From the repository root:

```bash
dotnet run --project CompaniesApi/CompaniesApi.csproj
```

The app listens on **http://localhost:5000** (see `CompaniesApi/Properties/launchSettings.json`). In **Development**, Swagger UI is at **http://localhost:5000/swagger**.

## Run with Docker

```bash
docker build -t companies-api .
docker run -p 5000:8080 companies-api
```

Then open **http://localhost:5000/swagger** to try the API (same URL as in [Try the API (Swagger)](#try-the-api-swagger)).

## Tests

```bash
dotnet test
```

## Try the API (Swagger)

With the app running, open **Swagger UI** at **http://localhost:5000/swagger** (Development; see [Run locally](#run-locally) or [Run with Docker](#run-with-docker)). Use **GET /companies/{id}** and **Try it out** — no `curl` needed.

Assuming the configured upstream is reachable, you can exercise the same cases as below:

| Case | `id` | Expected |
|------|------|----------|
| **200** — existing company | `1` | JSON for the first sample company |
| **200** — second sample | `2` | JSON for the second sample company |
| **400** — invalid id | `0` (or any non-positive integer) | **400** with `ApiError` |
| **404** — not found upstream | `999999` | **404** with `ApiError` |
| **502** — upstream/network/parse failure | (depends on upstream) | Returned when the upstream errors, the request times out, or XML cannot be parsed; invalid upstream XML typically yields **502** with an `Upstream Error` body |

Swagger shows response status and body for each call.

## Configuration

| Setting           | Purpose |
|------------------|---------|
| `UpstreamBaseUrl` | Base URL for `{id}.xml` requests (see `CompaniesApi/appsettings.json`). |

## Behavior summary

| Scenario                         | HTTP status | Body shape |
|----------------------------------|------------|------------|
| `id < 1`                         | 400        | `ApiError` (`error`, `error_description`) |
| Upstream 404                     | 404        | `ApiError` |
| Upstream 5xx, network, timeout, bad XML | 502 | `ApiError` |
| Success                          | 200        | `Company` (`id`, `name`, `description`) |

The HTTP client uses **Microsoft.Extensions.Http.Resilience** (`AddStandardResilienceHandler`) for retries and timeouts. Structured logs include upstream URL, status codes, and parse failures.
