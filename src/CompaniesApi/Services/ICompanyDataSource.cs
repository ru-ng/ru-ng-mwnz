namespace CompaniesApi.Services;

public interface ICompanyDataSource
{
    Task<RawDataResult> GetAsync(int id, CancellationToken cancellationToken = default);
}