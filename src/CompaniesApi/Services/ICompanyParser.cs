using CompaniesApi.Models;

namespace CompaniesApi.Services;

public interface ICompanyParser
{
    Company Parse(string rawContent);
}