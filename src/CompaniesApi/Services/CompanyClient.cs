using System.Net;
using System.Xml.Linq;
using CompaniesApi.Models;

namespace CompaniesApi.Services;

public sealed class CompanyClient(HttpClient httpClient, ILogger<CompanyClient> logger) : ICompanyClient
{
    public async Task<CompanyFetchResult> GetCompanyAsync(int id, CancellationToken cancellationToken = default)
    {
        var requestUri = $"{id}.xml";
        var upstreamUrl = new Uri(httpClient.BaseAddress!, requestUri).ToString();

        logger.LogInformation("Fetching company XML from {UpstreamUrl}", upstreamUrl);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error requesting {UpstreamUrl}", upstreamUrl);
            return new CompanyFetchResult(
                HttpStatusCode.BadGateway,
                null,
                $"Upstream request failed: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Timeout requesting {UpstreamUrl}", upstreamUrl);
            return new CompanyFetchResult(
                HttpStatusCode.BadGateway,
                null,
                "Upstream request timed out.");
        }

        logger.LogInformation(
            "Upstream responded with {StatusCode} for {UpstreamUrl}",
            (int)response.StatusCode,
            upstreamUrl);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new CompanyFetchResult(HttpStatusCode.NotFound, null, null);

        if (!response.IsSuccessStatusCode)
        {
            var reason = $"Upstream returned {(int)response.StatusCode} {response.ReasonPhrase}.";
            logger.LogWarning("Non-success from upstream: {Reason}", reason);
            return new CompanyFetchResult(response.StatusCode, null, reason);
        }

        string xml;
        try
        {
            xml = await ReadResponseContentAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed reading upstream body from {UpstreamUrl}", upstreamUrl);
            return new CompanyFetchResult(
                HttpStatusCode.BadGateway,
                null,
                "Failed reading upstream response body.");
        }

        try
        {
            var company = ParseCompanyFromXml(xml);
            return new CompanyFetchResult(HttpStatusCode.OK, company, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed parsing XML from {UpstreamUrl}", upstreamUrl);
            return new CompanyFetchResult(
                HttpStatusCode.BadGateway,
                null,
                $"Failed to parse upstream XML: {ex.Message}");
        }
    }

    private static async Task<string> ReadResponseContentAsync(HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        await response.Content.ReadAsStringAsync(cancellationToken);

    private static Company ParseCompanyFromXml(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new InvalidOperationException("Missing root element.");
        if (!string.Equals(root.Name.LocalName, "Data", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unexpected root element '{root.Name.LocalName}'.");

        var parsedId = int.Parse(root.Element("id")?.Value ?? throw new InvalidOperationException("Missing id."));
        var name = root.Element("name")?.Value ?? throw new InvalidOperationException("Missing name.");
        var description = root.Element("description")?.Value ??
                          throw new InvalidOperationException("Missing description.");

        return new Company(parsedId, name, description);
    }
}
