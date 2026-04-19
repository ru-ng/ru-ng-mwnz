namespace CompaniesApi.Services;

public interface ICompanyClient
{
    Task<CompanyFetchResult> GetCompanyAsync(int id, CancellationToken cancellationToken = default);
}
