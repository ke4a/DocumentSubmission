using DocumentSubmission.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentSubmission.Api.Errors;

/// <summary>The one place where exceptions become HTTP status codes (ProblemDetails, RFC 9457).</summary>
public sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    private const string ConcurrentChange = "The submission was changed by another request. Reload it and try again.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Message),
            NotAllowedException e => (StatusCodes.Status403Forbidden, "Forbidden", e.Message),
            InvalidInputException e => (StatusCodes.Status400BadRequest, "Invalid input", e.Message),
            InvalidStateException e => (StatusCodes.Status409Conflict, "Conflict", e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict", ConcurrentChange),
            DbUpdateException e when IsDuplicateDocumentVersion(e) => (StatusCodes.Status409Conflict, "Conflict", ConcurrentChange),
            BadHttpRequestException e => (e.StatusCode, "Bad request", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null),
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }

    // Two concurrent resubmits both insert version N+1; the second one violates the primary key.
    private static bool IsDuplicateDocumentVersion(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            TableName: "DocumentVersions",
        };
}
