using System.Net;
using CompaniesApi.Models;
using CompaniesApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompaniesApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CompaniesController(IXmlCompanyClient client, ILogger<CompaniesController> logger) : ControllerBase
{
    [HttpGet("{id:int}", Name = "GetCompany")]
    [ProducesResponseType<Company>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetCompany(int id, CancellationToken cancellationToken)
    {
        if (id < 1)
        {
            logger.LogWarning("Rejected invalid company id {CompanyId}", id);
            return BadRequest(new ApiError("Bad Request", "Company id must be a positive integer."));
        }

        var result = await client.GetCompanyAsync(id, cancellationToken);

        if (result.StatusCode == HttpStatusCode.NotFound)
            return NotFound(new ApiError("Not Found", $"Company {id} was not found."));

        if (result.StatusCode == HttpStatusCode.OK && result.Company is not null)
            return Ok(result.Company);

        var upstreamError = new ApiError(
            "Upstream Error",
            result.ErrorDescription ?? "Unexpected upstream response.");
        return StatusCode(StatusCodes.Status502BadGateway, upstreamError);
    }
}
