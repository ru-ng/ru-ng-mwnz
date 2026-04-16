using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CompaniesApi.Models;
using CompaniesApi.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CompaniesApi.Tests.Endpoints;

public sealed class CompaniesEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetCompany_ValidId_Returns200AndJsonCompany()
    {
        var stub = new TestXmlCompanyClient
        {
            Result = new CompanyFetchResult(
                HttpStatusCode.OK,
                new Company(1, "MWNZ", "..is awesome"),
                null)
        };

        await using var factory = CreateFactory(stub);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/companies/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var company = await response.Content.ReadFromJsonAsync<Company>(JsonOptions);
        Assert.NotNull(company);
        Assert.Equal(1, company.Id);
        Assert.Equal("MWNZ", company.Name);
        Assert.Equal("..is awesome", company.Description);
    }

    [Fact]
    public async Task GetCompany_InvalidId_Returns400AndApiError()
    {
        var stub = new TestXmlCompanyClient();
        await using var factory = CreateFactory(stub);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/companies/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal("Bad Request", error.Error);
        Assert.Contains("positive", error.ErrorDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCompany_NotFound_Returns404AndApiError()
    {
        var stub = new TestXmlCompanyClient
        {
            Result = new CompanyFetchResult(HttpStatusCode.NotFound, null, null)
        };

        await using var factory = CreateFactory(stub);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/companies/404");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal("Not Found", error.Error);
        Assert.Contains("404", error.ErrorDescription);
    }

    [Fact]
    public async Task GetCompany_UpstreamFailure_Returns502AndApiError()
    {
        var stub = new TestXmlCompanyClient
        {
            Result = new CompanyFetchResult(
                HttpStatusCode.BadGateway,
                null,
                "Upstream returned 500 Internal Server Error.")
        };

        await using var factory = CreateFactory(stub);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/companies/1");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal("Upstream Error", error.Error);
        Assert.NotNull(error.ErrorDescription);
    }

    private static WebApplicationFactory<Program> CreateFactory(TestXmlCompanyClient client)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.Single(d => d.ServiceType == typeof(IXmlCompanyClient));
                services.Remove(descriptor);
                services.AddSingleton<IXmlCompanyClient>(client);
            });
        });
    }

    private sealed class TestXmlCompanyClient : IXmlCompanyClient
    {
        public CompanyFetchResult Result { get; init; } =
            new(HttpStatusCode.OK, new Company(1, "x", "y"), null);

        public Task<CompanyFetchResult> GetCompanyAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
    }
}
