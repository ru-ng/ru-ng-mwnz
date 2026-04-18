---
name: Companies API — Take-home Evaluation
overview: Build an ASP.NET Core 9 Minimal API that fetches a company by ID from a static GitHub XML source, transforms the XML to JSON, and returns it — with resilience, structured logging, input validation, OpenAPI docs, and thorough tests.
todos:
  - id: scaffold
    content: Create solution with src/CompaniesApi, tests/CompaniesApi.Tests, and src/tests solution folders in the .sln
    status: pending
  - id: models
    content: Add Company and ApiError record models
    status: pending
  - id: client
    content: Implement IXmlCompanyClient and XmlCompanyClient (HttpClient + XML parsing)
    status: pending
  - id: resilience
    content: Add Polly retry + timeout policy on the typed HttpClient
    status: pending
  - id: validation
    content: Add input validation — reject non-positive IDs before hitting upstream
    status: pending
  - id: endpoint
    content: Register GET /companies/{id} endpoint in Program.cs with DI wiring and structured logging
    status: pending
  - id: openapi
    content: Add Swashbuckle OpenAPI/Swagger and host spec at /swagger
    status: pending
  - id: unit-tests
    content: Write XmlCompanyClientTests with mocked HttpClient
    status: pending
  - id: integration-tests
    content: Write CompaniesEndpointTests using WebApplicationFactory
    status: pending
  - id: dockerfile
    content: Add multi-stage Dockerfile (restore → build → publish → runtime)
    status: pending
  - id: readme
    content: Write README.md with prerequisites, run instructions, Swagger URL, and test command
    status: pending
isProject: false
---

# Companies API — Take-home Evaluation

## Overview
A single-endpoint ASP.NET Core 9 Minimal API that proxies `GET /companies/{id}` to the upstream XML service at `https://raw.githubusercontent.com/MiddlewareNewZealand/evaluation-instructions/main/xml-api/{id}.xml`, parses the XML, and returns JSON matching `openapi-companies.yaml`.

The four production-quality additions included to demonstrate engineering judgement:
1. **Resilience** — retry + timeout on the upstream HTTP call
2. **Structured logging** — request/error context via `ILogger<T>`
3. **Input validation** — guard invalid IDs before touching the upstream
4. **OpenAPI/Swagger** — auto-hosted spec at `/swagger`

## Upstream XML structure
The upstream returns XML with root element `<Data>`:

```xml
<Data>
  <id>1</id>
  <name>MWNZ</name>
  <description>..is awesome</description>
</Data>
```

## Project structure

Solution (`CompaniesApi.sln`) uses **solution folders** `src` and `tests` (virtual grouping; projects live on disk as below).

```
src/
  CompaniesApi/
    CompaniesApi.csproj        (.NET 9 minimal API)
    Program.cs                 (DI wiring, endpoint registration, Swagger)
    Models/
      Company.cs               (output JSON model)
      ApiError.cs              (error response model)
    Services/
      IXmlCompanyClient.cs     (interface for upstream HTTP calls)
      XmlCompanyClient.cs      (HttpClient + XML parsing + structured logging)
tests/
  CompaniesApi.Tests/
    CompaniesApi.Tests.csproj
    Services/
      XmlCompanyClientTests.cs (unit tests — mocked HttpClient)
    Endpoints/
      CompaniesEndpointTests.cs (integration tests via WebApplicationFactory)
```

## Key design decisions

- **Minimal API** — single `GET /companies/{id}` endpoint; concise and idiomatic .NET 9
- **`IXmlCompanyClient`** — interface isolates the upstream concern and enables clean mocking in tests
- **`XDocument`** — parses the `<Data>` root with `System.Xml.Linq`; no extra NuGet dependencies
- **`IHttpClientFactory`** — typed client (`AddHttpClient<XmlCompanyClient>`) with base URL in `appsettings.json`
- **Polly via `Microsoft.Extensions.Http.Resilience`** — `AddStandardResilienceHandler()` gives retry (3 attempts, exponential backoff) + 10s timeout out of the box; one line, no custom policy needed
- **Input validation** — return `400 Bad Request` before hitting the upstream if `id < 1`; shows guard-clause thinking
- **Structured logging** — log upstream URL, response status, and parse errors with semantic properties so logs are queryable
- **Swashbuckle** — `AddEndpointsApiExplorer()` + `AddSwaggerGen()` + `/swagger` middleware; demonstrates API design discipline

## Error handling

| Scenario | Status | Body |
|---|---|---|
| `id < 1` | `400` | `{ "error": "Bad Request", "error_description": "..." }` |
| Upstream 404 | `404` | `{ "error": "Not Found", "error_description": "Company {id} was not found." }` |
| Upstream 5xx / network error | `502` | `{ "error": "Upstream Error", "error_description": "..." }` |
| Unparseable XML | `502` | `{ "error": "Upstream Error", "error_description": "..." }` |

## Models

```csharp
// Company.cs
public record Company(int Id, string Name, string Description);

// ApiError.cs  — property names use JSON snake_case via JsonPropertyName
public record ApiError(string Error, string ErrorDescription);
```

## Resilience (Program.cs wiring)

```csharp
builder.Services.AddHttpClient<XmlCompanyClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["UpstreamBaseUrl"]!))
    .AddStandardResilienceHandler();  // retry + timeout from Polly
```

## Swagger (Program.cs wiring)

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

## Tests
- **Unit tests** (`XmlCompanyClientTests`): mock `HttpMessageHandler`, assert correct parsing of valid XML, 404 handling, non-success status codes, and malformed XML
- **Integration tests** (`CompaniesEndpointTests`): use `WebApplicationFactory`, replace `IXmlCompanyClient` with a fake, verify full HTTP response shape (status codes, JSON bodies, `Content-Type` header) for all scenarios including the `400` validation path

## Dockerfile
Multi-stage build so the evaluator only needs Docker (not .NET SDK):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/CompaniesApi/CompaniesApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CompaniesApi.dll"]
```

Run with:
```bash
docker build -t companies-api .
docker run -p 5000:8080 companies-api
```

## README
Covers:
- Prerequisites: .NET 9 SDK **or** Docker (either works)
- `dotnet run --project src/CompaniesApi/CompaniesApi.csproj` with Swagger URL
- `docker build` + `docker run` path
- `dotnet test` command
- Swagger UI at `/swagger` for trying all scenarios (200, 404, 400, second valid company, 502 notes)


