namespace Catalog.Api.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? TraceId { get; set; }

    // Exposed for validation errors (400): field -> messages[]
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    // Success response
    public ApiResponse(T? data, string? message = null, string? traceId = null)
    {
        Success = true;
        Data    = data;
        Message = message;
        TraceId = traceId;
    }

    // Error response (used by endpoints for manual error returns)
    public ApiResponse(ApiError error, string? traceId = null)
    {
        Success = false;
        Message = error.Description;
        TraceId = traceId;
    }

    // Parameterless constructor used by ExceptionHandlingMiddleware
    public ApiResponse() { }
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}