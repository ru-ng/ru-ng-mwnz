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

Then open **http://localhost:5000/swagger** (or call the API on port 5000 as in the examples below).

## Tests

```bash
dotnet test
```

## Examples (curl)

Assuming the app is at `http://localhost:5000` and the configured upstream is reachable:

**200 — existing company (id 1):**

```bash
curl -s http://localhost:5000/companies/1
```

**200 — second sample company (id 2):**

```bash
curl -s http://localhost:5000/companies/2
```

**400 — invalid id (not positive):**

```bash
curl -s -i http://localhost:5000/companies/0
```

**404 — company not found upstream:**

```bash
curl -s -i http://localhost:5000/companies/999999
```

**502 — upstream/network/parse failure:** these are returned when the upstream returns an error status, the request times out, or the XML cannot be parsed. Triggering one depends on upstream behavior; an invalid XML response from the upstream would surface as **502** with an `Upstream Error` body.

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
