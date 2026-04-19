using System.Net;

namespace CompaniesApi.Services;

public sealed class CompanyClient(ICompanyDataSource dataSource, ICompanyParser parser, ILogger<CompanyClient> logger) : ICompanyClient
{
    public async Task<CompanyResult> GetCompanyAsync(int id, CancellationToken cancellationToken = default)
    {
        var rawResult = await dataSource.GetAsync(id, cancellationToken);

        if (rawResult.Status == RawDataStatus.NotFound)
            return new CompanyResult(HttpStatusCode.NotFound, null, null);

        if (rawResult.Status == RawDataStatus.Error)
            return new CompanyResult(HttpStatusCode.BadGateway, null, rawResult.ErrorDescription);

        try
        {
            var company = parser.Parse(rawResult.Content!);
            return new CompanyResult(HttpStatusCode.OK, company, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed parsing content for company Id {Id}", id);
            return new CompanyResult(HttpStatusCode.BadGateway, null, $"Failed to parse content: {ex.Message}");
        }
    }
}
