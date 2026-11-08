namespace SchoolAdmission.Api.Infrastructure;

/// <summary>
/// A business-rule failure that maps to a specific HTTP status and error code.
/// Thrown by services, turned into an <c>ErrorResponse</c> by the middleware.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public ApiException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public static ApiException BadRequest(string code, string message) => new(400, code, message);
    public static ApiException Forbidden(string code, string message) => new(403, code, message);
    public static ApiException NotFound(string code, string message) => new(404, code, message);
    public static ApiException Conflict(string code, string message) => new(409, code, message);
}

/// <summary>All error codes in one place (also listed in README).</summary>
public static class ErrorCodes
{
    public const string InvalidInput = "INVALID_INPUT";
    public const string InvalidStudentId = "INVALID_STUDENT_ID";
    public const string ClassNotFound = "CLASS_NOT_FOUND";
    public const string HoldNotFound = "HOLD_NOT_FOUND";
    public const string NotYourHold = "NOT_YOUR_HOLD";
    public const string SeatsFull = "SEATS_FULL";
    public const string AlreadyApplied = "ALREADY_APPLIED";
    public const string HoldExpired = "HOLD_EXPIRED";
    public const string HoldCancelled = "HOLD_CANCELLED";
    public const string AlreadyConfirmed = "ALREADY_CONFIRMED";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string InternalError = "INTERNAL_ERROR";
}
