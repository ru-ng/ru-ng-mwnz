namespace CompaniesApi.Services;

public interface IXmlCompanyClient
{
    Task<CompanyFetchResult> GetCompanyAsync(int id, CancellationToken cancellationToken = default);
}
