using System.Net;
using System.Text;
using CompaniesApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CompaniesApi.Tests.Services;

public sealed class XmlCompanyClientTests
{
    private static XmlCompanyClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/xml-api/")
        };
        return new XmlCompanyClient(httpClient, NullLogger<XmlCompanyClient>.Instance);
    }

    [Fact]
    public async Task GetCompanyAsync_ValidXml_ReturnsCompany()
    {
        const string xml = """
            <Data>
              <id>1</id>
              <name>MWNZ</name>
              <description>..is awesome</description>
            </Data>
            """;

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var sut = CreateClient(handler);
        var result = await sut.GetCompanyAsync(1);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        ((object?)result.Company).Should().NotBeNull();
        result.Company!.Id.Should().Be(1);
        result.Company.Name.Should().Be("MWNZ");
        result.Company.Description.Should().Be("..is awesome");
        result.ErrorDescription.Should().BeNull();
    }

    [Fact]
    public async Task GetCompanyAsync_NotFound_Returns404()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.GetCompanyAsync(99);

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ((object?)result.Company).Should().BeNull();
    }

    [Fact]
    public async Task GetCompanyAsync_UpstreamError_ReturnsFailureResult()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateClient(handler);

        var result = await sut.GetCompanyAsync(1);

        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ((object?)result.Company).Should().BeNull();
        result.ErrorDescription.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCompanyAsync_MalformedXml_Returns502WithDescription()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not xml", Encoding.UTF8, "application/xml")
        });
        var sut = CreateClient(handler);

        var result = await sut.GetCompanyAsync(1);

        result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        ((object?)result.Company).Should().BeNull();
        result.ErrorDescription.Should().NotBeNull().And.ContainEquivalentOf("parse");
    }

    [Fact]
    public async Task GetCompanyAsync_InvalidRootElement_Returns502()
    {
        const string xml = "<Wrong><id>1</id></Wrong>";
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });
        var sut = CreateClient(handler);

        var result = await sut.GetCompanyAsync(1);

        result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        ((object?)result.Company).Should().BeNull();
    }

    [Fact]
    public async Task GetCompanyAsync_HttpRequestException_Returns502()
    {
        var handler = new ThrowingHandler(new HttpRequestException("connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.GetCompanyAsync(1);

        result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        ((object?)result.Company).Should().BeNull();
        result.ErrorDescription.Should().NotBeNull().And.Contain("Upstream request failed");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_respond(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _ex;

        public ThrowingHandler(Exception ex) => _ex = ex;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(_ex);
    }
}
