using System.Net;
using CompaniesApi.Models;

namespace CompaniesApi.Services;

public sealed record CompanyFetchResult(
    HttpStatusCode StatusCode,
    Company? Company,
    string? ErrorDescription);
