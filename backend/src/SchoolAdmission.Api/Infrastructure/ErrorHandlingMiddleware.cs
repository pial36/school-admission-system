using Npgsql;
using SchoolAdmission.Api.Contracts;

namespace SchoolAdmission.Api.Infrastructure;

/// <summary>
/// Turns every exception into a JSON <see cref="ErrorResponse"/> so the UI never
/// gets an empty body or an HTML error page.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await Write(context, ex.StatusCode, new ErrorResponse(ex.Code, ex.Message));
        }
        catch (Exception ex) when (IsDatabaseUnavailable(ex))
        {
            _logger.LogError(ex, "Database unavailable");
            await Write(context, StatusCodes.Status503ServiceUnavailable, new ErrorResponse(
                ErrorCodes.ServiceUnavailable,
                "The admission service is temporarily unavailable. Please try again in a moment."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");
            await Write(context, StatusCodes.Status500InternalServerError, new ErrorResponse(
                ErrorCodes.InternalError,
                "Something went wrong on our side. Please try again."));
        }
    }

    private static bool IsDatabaseUnavailable(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is NpgsqlException { IsTransient: true } or System.Net.Sockets.SocketException or TimeoutException)
                return true;
        }
        return false;
    }

    private static Task Write(HttpContext context, int status, ErrorResponse body)
    {
        if (context.Response.HasStarted) return Task.CompletedTask;
        context.Response.Clear();
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(body);
    }
}
