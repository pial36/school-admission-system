using System.Text.RegularExpressions;

namespace SchoolAdmission.Api.Infrastructure;

/// <summary>Reads and validates the X-Student-Id header (17-digit birth registration number).</summary>
public static partial class StudentIdHeader
{
    public const string Name = "X-Student-Id";

    [GeneratedRegex(@"^\d{17}$")]
    private static partial Regex SeventeenDigits();

    public static string Require(HttpRequest request)
    {
        var value = request.Headers[Name].ToString().Trim();

        if (string.IsNullOrEmpty(value))
            throw ApiException.BadRequest(ErrorCodes.InvalidStudentId,
                "Please enter the student's birth registration number.");

        if (!SeventeenDigits().IsMatch(value))
            throw ApiException.BadRequest(ErrorCodes.InvalidStudentId,
                "Birth registration number must be exactly 17 digits.");

        return value;
    }
}
