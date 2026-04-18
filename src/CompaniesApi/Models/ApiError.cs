using System.Text.Json.Serialization;

namespace CompaniesApi.Models;

public record ApiError(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("error_description")] string ErrorDescription);
