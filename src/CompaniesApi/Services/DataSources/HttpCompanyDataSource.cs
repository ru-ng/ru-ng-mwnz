using System.Net;

namespace CompaniesApi.Services.DataSources;

public sealed class HttpCompanyDataSource(HttpClient httpClient, ILogger<HttpCompanyDataSource> logger) : ICompanyDataSource
{
    public async Task<RawDataResult> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var requestUri = $"{id}.xml";
        var upstreamUrl = new Uri(httpClient.BaseAddress!, requestUri).ToString();

        logger.LogInformation("Getting company XML from {UpstreamUrl}", upstreamUrl);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error requesting {UpstreamUrl}", upstreamUrl);
            return new RawDataResult(RawDataStatus.Error, null, $"Upstream request failed: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Timeout requesting {UpstreamUrl}", upstreamUrl);
            return new RawDataResult(RawDataStatus.Error, null, "Upstream request timed out.");
        }

        logger.LogInformation(
            "Upstream responded with {StatusCode} for {UpstreamUrl}",
            (int)response.StatusCode,
            upstreamUrl);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new RawDataResult(RawDataStatus.NotFound, null, null);

        if (!response.IsSuccessStatusCode)
        {
            var reason = $"Upstream returned {(int)response.StatusCode} {response.ReasonPhrase}.";
            logger.LogWarning("Non-success from upstream: {Reason}", reason);
            return new RawDataResult(RawDataStatus.Found, null, reason);
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return new RawDataResult(RawDataStatus.Found, content, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed reading upstream body from {UpstreamUrl}", upstreamUrl);
            return new RawDataResult(RawDataStatus.Error, null, "Failed reading upstream response body.");
        }
    }
}