using System.Net;
using CompaniesApi.Models;

namespace CompaniesApi.Services;

public sealed record CompanyResult(
    HttpStatusCode StatusCode,
    Company? Company,
    string? ErrorDescription);
