using System.Text.Json;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Shared.Infrastructure.Interfaces.ASP.Middleware;

/// <summary>
///     Converts exceptions into RFC 7807 <c>application/problem+json</c> responses with an extra
///     <c>code</c> field. Stack traces are never returned to the client.
/// </summary>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            logger.LogInformation("Business rule rejected {Method} {Path}: {Code}",
                context.Request.Method, context.Request.Path, exception.Code);
            await WriteProblemAsync(context, ToStatusCode(exception.Kind), exception.Code, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "concurrency-conflict",
                "The resource was modified by another operation. Reload the data and try again.");
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            logger.LogWarning("Unique constraint violation on {Method} {Path}", context.Request.Method,
                context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "duplicate-value",
                "A record with the same unique value already exists.");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {Path} was cancelled by the client", context.Request.Path);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error while processing {Method} {Path}", context.Request.Method,
                context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "internal-error",
                "The request could not be completed. Check the server logs using the traceId.");
        }
    }

    private static int ToStatusCode(DomainErrorKind kind)
    {
        return kind switch
        {
            DomainErrorKind.BadRequest => StatusCodes.Status400BadRequest,
            DomainErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            DomainErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            DomainErrorKind.NotFound => StatusCodes.Status404NotFound,
            DomainErrorKind.Conflict => StatusCodes.Status409Conflict,
            DomainErrorKind.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
               || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string code, string detail)
    {
        if (context.Response.HasStarted) return;

        // Headers are not cleared on purpose: the CORS headers already added must be kept so that the
        // browser lets the Web Application read the error.
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrase(statusCode),
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }

    private static string ReasonPhrase(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            _ => "Internal Server Error"
        };
    }
}
