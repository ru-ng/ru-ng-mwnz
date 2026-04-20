using System.Net;
using System.Text;
using CompaniesApi.Services;
using CompaniesApi.Services.DataSources;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CompaniesApi.Tests.Services;

public sealed class HttpCompanyDataSourceTests
{
    private static HttpCompanyDataSource CreateDataSource(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/xml-api/") };
        return new HttpCompanyDataSource(httpClient, NullLogger<HttpCompanyDataSource>.Instance);
    }

    [Fact]
    public async Task GetAsync_SuccessResponse_ReturnsFoundWithContent()
    {
        const string xml = "<Data><id>1</id></Data>";
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var result = await CreateDataSource(handler).GetAsync(1);

        result.Status.Should().Be(RawDataStatus.Found);
        result.Content.Should().Be(xml);
        result.ErrorDescription.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_NotFoundResponse_ReturnsNotFound()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await CreateDataSource(handler).GetAsync(99);

        result.Status.Should().Be(RawDataStatus.NotFound);
        result.Content.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_UpstreamServerError_ReturnsError()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await CreateDataSource(handler).GetAsync(1);

        result.Status.Should().Be(RawDataStatus.Error);
        result.ErrorDescription.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_HttpRequestException_ReturnsError()
    {
        var handler = new ThrowingHandler(new HttpRequestException("connection refused"));

        var result = await CreateDataSource(handler).GetAsync(1);

        result.Status.Should().Be(RawDataStatus.Error);
        result.ErrorDescription.Should().NotBeNull().And.Contain("Upstream request failed");
    }
    
    [Fact]
    public async Task GetAsync_HttpRequestTimeout_ReturnsError()
    {
        var handler = new ThrowingHandler(new TaskCanceledException("timeout"));

        var result = await CreateDataSource(handler).GetAsync(1);

        result.Status.Should().Be(RawDataStatus.Error);
        result.ErrorDescription.Should().NotBeNull().And.Contain("Upstream request timed out");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class ThrowingHandler(Exception ex) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(ex);
    }
}
