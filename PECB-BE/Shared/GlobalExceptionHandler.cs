using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using PECB_BE.Shared.Response;

namespace PECB_BE.Shared;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var description = "Server.Error";
        var detail = "An unexpected error occurred on the server.";
        
        if (exception is InvalidOperationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            description = "Server.InvalidOperation";
            detail = exception.Message;
        }

        if (exception is ArgumentOutOfRangeException)
        {
            statusCode = HttpStatusCode.Conflict;
            description = "Server.ArgumentOutOfRangeException";
            detail = "Given enum not according to the values for the enum selection";
        }
        
        var errorResponse = new Error(
            statusCode,
            description,
            detail
        );
        
        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        
        return true; 
        
    }
}