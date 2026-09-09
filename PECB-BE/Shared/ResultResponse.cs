using System.Net;
using PECB_BE.Shared.Response;

namespace PECB_BE.Shared;

public class ResultResponse<T> : IResult
{
    private readonly Success<T>? _success;
    private readonly Error? _error;
    private readonly HttpStatusCode _status;

    private ResultResponse(T data, HttpStatusCode status, string? description)
    {
        _success = new Success<T>(data, description);
        _status = status;
    }
    
    private ResultResponse(Error error)
    {
        _error = error;
        _status = error.Status ?? HttpStatusCode.InternalServerError;
    }
    
    public static ResultResponse<T> Success(T data, HttpStatusCode status = HttpStatusCode.OK, string? description = null)
        => new(data, status, description);
    
    public static ResultResponse<T> Failure(Error error)
        => new(error);

    // Let a service layer return ResultResponse<T> and have the caller branch on it
    // (if (!result.IsSuccess) { ... }) before mapping into its own ResultResponse.
    public bool IsSuccess => _error is null;

    public Error? Error => _error;

    public T? Value => _success is not null ? _success.Data : default;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = (int)_status;

        if (_error is null)
            await Results.Json(_success, statusCode: (int)_status).ExecuteAsync(httpContext);
        else
            await Results.Json(_error, statusCode: (int)_status).ExecuteAsync(httpContext);
    }
}