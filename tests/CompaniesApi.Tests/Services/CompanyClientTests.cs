using System.Net;
using CompaniesApi.Models;
using CompaniesApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CompaniesApi.Tests.Services;

public sealed class CompanyClientTests
{
    private static readonly Company SomeCompany = new(1, "MWNZ", "..is awesome");

    private static CompanyClient CreateClient(ICompanyDataSource dataSource, ICompanyParser parser) => 
        new(dataSource, parser, NullLogger<CompanyClient>.Instance);

    [Fact]
    public async Task GetCompanyAsync_DataSourceReturnsFound_ReturnsCompany()
    {
        var sut = CreateClient(
            new StubDataSource(new RawDataResult(RawDataStatus.Found, "SomeContent", null)),
            new StubParser(SomeCompany));

        var result = await sut.GetCompanyAsync(1);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Company.Should().NotBeNull();
        result.Company!.Id.Should().Be(1);
        result.Company.Name.Should().Be("MWNZ");
        result.Company.Description.Should().Be("..is awesome");
        result.ErrorDescription.Should().BeNull();
    }

    [Fact]
    public async Task GetCompanyAsync_DataSourceReturnsNotFound_Returns404()
    {
        var sut = CreateClient(
            new StubDataSource(new RawDataResult(RawDataStatus.NotFound, null, null)),
            new StubParser(SomeCompany));

        var result = await sut.GetCompanyAsync(99);

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.Company.Should().BeNull();
    }

    [Fact]
    public async Task GetCompanyAsync_DataSourceReturnsError_ReturnsBadGatewayWithDescription()
    {
        var sut = CreateClient(
            new StubDataSource(new RawDataResult(RawDataStatus.Error, null, "some error")),
            new StubParser(SomeCompany));

        var result = await sut.GetCompanyAsync(1);

        result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        result.Company.Should().BeNull();
        result.ErrorDescription.Should().Be("some error");
    }

    [Fact]
    public async Task GetCompanyAsync_ParserThrows_ReturnsBadGateWayWithDescription()
    {
        var sut = CreateClient(
            new StubDataSource(new RawDataResult(RawDataStatus.Found, "SomeContent", null)),
            new ThrowingParser(new InvalidOperationException("Missing id.")));

        var result = await sut.GetCompanyAsync(1);

        result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        result.Company.Should().BeNull();
        result.ErrorDescription.Should().NotBeNull().And.ContainEquivalentOf("parse");
    }

    private sealed class StubDataSource(RawDataResult result) : ICompanyDataSource
    {
        public Task<RawDataResult> GetAsync(int id, CancellationToken cancellationToken = default) => 
            Task.FromResult(result);
    }

    private sealed class StubParser(Company company) : ICompanyParser
    {
        public Company Parse(string rawContent) => company;
    }

    private sealed class ThrowingParser(Exception ex) : ICompanyParser
    {
        public Company Parse(string rawContent) => throw ex;
    }
}
