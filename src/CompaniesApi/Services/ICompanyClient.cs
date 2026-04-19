namespace CompaniesApi.Services;

public interface ICompanyClient
{
    Task<CompanyResult> GetCompanyAsync(int id, CancellationToken cancellationToken = default);
}
