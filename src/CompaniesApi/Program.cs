using System.Net;
using System.Text.Json;
using CompaniesApi.Models;
using CompaniesApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var upstreamBaseUrl = builder.Configuration["UpstreamBaseUrl"]
    ?? throw new InvalidOperationException("UpstreamBaseUrl is not configured.");

builder.Services.AddHttpClient<XmlCompanyClient>(client =>
    {
        client.BaseAddress = new Uri(upstreamBaseUrl);
    })
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IXmlCompanyClient>(sp => sp.GetRequiredService<XmlCompanyClient>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/companies/{id:int}", async (
        int id,
        IXmlCompanyClient client,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
        await HandleGetCompany(id, client, loggerFactory, cancellationToken))
    .WithName("GetCompany");

app.Run();

static async Task<Results<Ok<Company>, BadRequest<ApiError>, NotFound<ApiError>, JsonHttpResult<ApiError>>> HandleGetCompany(
    int id,
    IXmlCompanyClient client,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("CompaniesApi.Endpoints.Companies");

    if (id < 1)
    {
        logger.LogWarning("Rejected invalid company id {CompanyId}", id);
        var badRequest = new ApiError(
            "Bad Request",
            "Company id must be a positive integer.");
        return TypedResults.BadRequest(badRequest);
    }

    var result = await client.GetCompanyAsync(id, cancellationToken);

    if (result.StatusCode == HttpStatusCode.NotFound)
    {
        var notFound = new ApiError(
            "Not Found",
            $"Company {id} was not found.");
        return TypedResults.NotFound(notFound);
    }

    if (result.StatusCode == HttpStatusCode.OK && result.Company is not null)
    {
        return TypedResults.Ok(result.Company);
    }

    var upstreamError = new ApiError(
        "Upstream Error",
        result.ErrorDescription ?? "Unexpected upstream response.");
    return TypedResults.Json(upstreamError, statusCode: StatusCodes.Status502BadGateway);
}

public partial class Program;
