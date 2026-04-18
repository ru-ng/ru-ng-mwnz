using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CompaniesApi.Models;
using CompaniesApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var company = await response.Content.ReadFromJsonAsync<Company>(JsonOptions);
        company.Should().NotBeNull();
        company!.Id.Should().Be(1);
        company.Name.Should().Be("MWNZ");
        company.Description.Should().Be("..is awesome");
    }

    [Fact]
    public async Task GetCompany_InvalidId_Returns400AndApiError()
    {
        var stub = new TestXmlCompanyClient();
        await using var factory = CreateFactory(stub);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/companies/0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
        error.Should().NotBeNull();
        error!.Error.Should().Be("Bad Request");
        error.ErrorDescription.Should().ContainEquivalentOf("positive");
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
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
        error.Should().NotBeNull();
        error!.Error.Should().Be("Not Found");
        error.ErrorDescription.Should().Contain("404");
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
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

        var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
        error.Should().NotBeNull();
        error!.Error.Should().Be("Upstream Error");
        error.ErrorDescription.Should().NotBeNull();
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
