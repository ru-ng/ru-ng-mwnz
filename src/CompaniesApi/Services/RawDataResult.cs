namespace CompaniesApi.Services;

public enum RawDataStatus {Found, NotFound, Error}

// Transport agnostic result type
public sealed record RawDataResult(
    RawDataStatus Status,
    string? Content,
    string? ErrorDescription
);